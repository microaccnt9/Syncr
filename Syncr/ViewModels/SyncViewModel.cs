using System;

using Syncr.Helpers;
using System.Windows.Input;
using Windows.Storage;
using System.Threading;
using System.Threading.Tasks;
using Syncr.Services;
using System.Linq;
using System.IO;
using Windows.Storage.Search;
using FlickrNet;
using System.Collections.Generic;
using static Syncr.Services.SettingsService;

namespace Syncr.ViewModels
{
    public class SyncViewModel : BackgroundThreadObservable
    {
        private string[] extensions = { ".jpeg", ".jpg", ".png", ".gif", ".mp4", ".avi", ".wmv", ".mov", ".mpeg", ".mpg", ".m2v", ".3gp", ".m2ts", ".ogg", ".ogv" };
        private StorageFolder syncFolder;

        public StorageFolder SyncFolder
        {
            get { return syncFolder; }
            set { Set(ref syncFolder, value); }
        }

        private string currentOperationDescription;

        public string CurrentOperationDescription
        {
            get { return currentOperationDescription; }
            set { Set(ref currentOperationDescription, value); }
        }

        private double progressValue;

        public double ProgressValue
        {
            get { return progressValue; }
            set { Set(ref progressValue, value); }
        }

        private double progressMax;

        public double ProgressMax
        {
            get { return progressMax; }
            set { Set(ref progressMax, value); }
        }


        private ICommand cancelCommand;

        public ICommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(Cancel, () => isCancelRequested == 0);
                }
                return cancelCommand;
            }
        }

        public Task ProcessingTask { get; private set; }

        private int isCancelRequested;

        private void Cancel()
        {
            Interlocked.Exchange(ref isCancelRequested, 1);
            OnPropertyChanged(nameof(CancelCommand));
        }

        internal void StartProcessing()
        {
            ProcessingTask = Task.Factory.StartNew(SynchronizeAsync);
            ProcessingTask.ContinueWith(t =>  OnUiThread(() => NavigationService.GoBack()));
        }

        private async void SynchronizeAsync()
        {
            var flickr = Singleton<FlickrService>.Instance.FlickrNet;
            var functionMode = Singleton<SettingsService>.Instance.Mode;

            CurrentOperationDescription = $"Parsing folder \"{SyncFolder.Path}\"";
            var queryResult = SyncFolder.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.DefaultQuery, extensions) { FolderDepth = FolderDepth.Deep });
            var files = await queryResult.GetFilesAsync();
            ProgressMax = files.Count;

            var photosetsList = await flickr.PhotosetsGetListAsync();
            var photosets = photosetsList.Where(ps => !string.IsNullOrWhiteSpace(ps.Description) && ps.Description[0] == '`')
                .Distinct(new GenericEqualityComparer<Photoset>(ps => ps.Description)).ToDictionary(ps => ps.Description);
            var groupedFiles = files.GroupBy(f => Path.GetDirectoryName(f.Path));
            foreach (var group in groupedFiles)
            {
                var photosetName = Path.GetFileName(group.Key);
                var photosetDescription = "`" + group.Key.Replace(SyncFolder.Path, ".");
                Dictionary<string, Photo> photos;
                if (photosets.TryGetValue(photosetDescription, out Photoset photoset))
                {
                    var photosCollection = await flickr.PhotosetsGetPhotosAsync(photoset.PhotosetId);
                    photos = photosCollection.Distinct(new GenericEqualityComparer<Photo>(p => p.Title)).ToDictionary(p => p.Title);
                }
                else
                {
                    photos = new Dictionary<string, Photo>();
                }

                foreach (var file in group)
                {
                    if (photos.Remove(file.Name))
                    {
                        ProgressValue++;
                        continue;
                    }

                    if (functionMode != FunctionMode.DownloadOnly)
                    {
                        CurrentOperationDescription = $"Uploading file \"{file.Path}\"";
                        string photoId = null;
                        using (var stream = (await file.OpenSequentialReadAsync()).AsStreamForRead())
                        {
                            photoId = await flickr.UploadPictureAsync(stream, file.Name, file.Name, file.Path.Replace(SyncFolder.Path, "."), "", false, false, false, ContentType.Photo, SafetyLevel.None, HiddenFromSearch.Hidden);
                        }

                        if (photoset == null)
                        {
                            CurrentOperationDescription = $"Creating photoset \"{photosetName}\"";
                            photoset = await flickr.PhotosetsCreateAsync(photosetName, photosetDescription, photoId);
                        }
                        else
                        {
                            CurrentOperationDescription = $"Adding file \"{file.Name}\" to photoset \"{photosetName}\"";
                            await flickr.PhotosetsAddPhotoAsync(photoset.PhotosetId, photoId);
                        }
                    }

                    ProgressValue++;
                }

                if (photos.Count > 0 && functionMode != FunctionMode.UploadOnly)
                {
                    ProgressMax += photos.Count;
                    var folder = await StorageFolder.GetFolderFromPathAsync(group.Key);
                    foreach (var photo in photos.Values.Where(p => !p.CanDownload.HasValue || p.CanDownload.Value))
                    {
                        CurrentOperationDescription = $"Downloading {photo.Title}";
                        await flickr.DownloadFileAsync(folder, photo);
                        ProgressValue++;
                    }
                }
                photosets.Remove(photosetDescription);
            }
            if (functionMode != FunctionMode.UploadOnly)
            {
                foreach (var photoset in photosets.Values)
                {
                    var folder = await CreateFolderRecursivelyAsync(SyncFolder, photoset.Description.Substring(1));
                    var photos = await flickr.PhotosetsGetPhotosAsync(photoset.PhotosetId);
                    ProgressMax += photos.Count;
                    foreach (var photo in photos.Where(p => !p.CanDownload.HasValue || p.CanDownload.Value))
                    {
                        CurrentOperationDescription = $"Downloading {photo.Title}";
                        await flickr.DownloadFileAsync(folder, photo);
                        ProgressValue++;
                    }
                }
            }

            ProgressValue = ProgressMax;
            CurrentOperationDescription = "Finished.";
        }

        private async Task<StorageFolder> CreateFolderRecursivelyAsync(StorageFolder parent, string path)
        {
            foreach (var folderName in path.Split(Path.DirectorySeparatorChar))
            {
                parent = await parent.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
            }
            return parent;
        }
    }
}
