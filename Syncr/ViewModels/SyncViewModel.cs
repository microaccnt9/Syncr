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
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.FileProperties;

namespace Syncr.ViewModels
{
    public class SyncViewModel : BackgroundThreadObservable
    {
        private string[] extensions = { ".jpeg", ".jpg", ".png", ".gif", /*".mp4", ".avi", ".wmv", ".mov", ".mpeg", ".mpg", ".m2v", ".3gp", ".m2ts", ".ogg", ".ogv"*/ };
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

        private BitmapImage previewImage = new BitmapImage();

        public BitmapImage PreviewImage
        {
            get { return previewImage; }
            set { Set(ref previewImage, value); }
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

        private bool isProgressIndeterminate;

        public bool IsProgressIndeterminate
        {
            get { return isProgressIndeterminate; }
            set { Set(ref isProgressIndeterminate, value); }
        }

        private ICommand cancelCommand;

        public ICommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(Cancel, () => !cancellationTokenSource.IsCancellationRequested);
                }
                return cancelCommand;
            }
        }

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private void Cancel()
        {
            CurrentOperationDescription = "Sync_CancellingStatus".GetLocalized();
            IsProgressIndeterminate = true;
            cancellationTokenSource.Cancel();
            OnPropertyChanged(nameof(CancelCommand));
        }

        internal void StartProcessing()
        {
            SynchronizeAsync(cancellationTokenSource.Token).ContinueWith(t =>  OnUiThread(() => NavigationService.GoBack()));
        }

        private async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            var flickr = Singleton<FlickrService>.Instance.FlickrNet;
            var functionMode = Singleton<SettingsService>.Instance.Mode;

            try
            {
                CurrentOperationDescription = string.Format("Sync_ParsingFolderStatus".GetLocalized(), SyncFolder.Path);
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, extensions) { FolderDepth = FolderDepth.Deep };
                var thumbnailMode = ThumbnailMode.SingleItem;
                uint thumbnailSize = 480u;
                var thumbnailOption = ThumbnailOptions.ResizeThumbnail;
                queryOptions.SetThumbnailPrefetch(thumbnailMode, thumbnailSize, thumbnailOption);
                var queryResult = SyncFolder.CreateFileQueryWithOptions(queryOptions);
                var files = await queryResult.GetFilesAsync();
                cancellationToken.ThrowIfCancellationRequested();
                ProgressMax = files.Count;

                var photosetsList = await flickr.PhotosetsGetListAsync();
                cancellationToken.ThrowIfCancellationRequested();
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
                            CurrentOperationDescription = string.Format("Sync_UploadingFileStatus".GetLocalized(), file.Path);
                            var thumbnail = await file.GetThumbnailAsync(thumbnailMode, thumbnailSize, thumbnailOption);
                            using (var stream = thumbnail.AsStreamForRead().AsRandomAccessStream())
                            {
                                await PreviewImage.SetSourceAsync(stream);
                            }
                            string photoId = null;
                            using (var stream = (await file.OpenSequentialReadAsync()).AsStreamForRead())
                            {
                                photoId = await flickr.UploadPictureAsync(stream, file.Name, file.Name, file.Path.Replace(SyncFolder.Path, "."), "", false, false, false, ContentType.Photo, SafetyLevel.None, HiddenFromSearch.Hidden);
                            }

                            if (photoset == null)
                            {
                                CurrentOperationDescription = string.Format("Sync_CreatingPhotoSetStatus".GetLocalized(), photosetName);
                                photoset = await flickr.PhotosetsCreateAsync(photosetName, photosetDescription, photoId);
                            }
                            else
                            {
                                CurrentOperationDescription = string.Format("Sync_AddingFileToPhotoSet".GetLocalized(), file.Name, photosetName);
                                await flickr.PhotosetsAddPhotoAsync(photoset.PhotosetId, photoId);
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        ProgressValue++;
                    }

                    if (photos.Count > 0 && functionMode != FunctionMode.UploadOnly)
                    {
                        ProgressMax += photos.Count;
                        var folder = await StorageFolder.GetFolderFromPathAsync(group.Key);
                        foreach (var photo in photos.Values.Where(p => !p.CanDownload.HasValue || p.CanDownload.Value))
                        {
                            CurrentOperationDescription = string.Format("Sync_DownloadingFile".GetLocalized(), photo.Title);
                            var sizes = await flickr.PhotosGetSizesAsync(photo.PhotoId);
                            PreviewImage.UriSource = new Uri(sizes.OrderByDescending(size => Math.Abs(size.Width - thumbnailSize)).First().Url);
                            await flickr.DownloadFileAsync(folder, photo.Title, sizes, cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();
                            ProgressValue++;
                        }
                    }
                    photosets.Remove(photosetDescription);
                }
                if (functionMode != FunctionMode.UploadOnly)
                {
                    foreach (var photoset in photosets.Values)
                    {
                        var folder = await CreateFolderRecursivelyAsync(SyncFolder, photoset.Description.Substring(1), cancellationToken);
                        var photos = await flickr.PhotosetsGetPhotosAsync(photoset.PhotosetId);
                        ProgressMax += photos.Count;
                        foreach (var photo in photos.Where(p => !p.CanDownload.HasValue || p.CanDownload.Value))
                        {
                            CurrentOperationDescription = string.Format("Sync_DownloadingFile".GetLocalized(), photo.Title);
                            var sizes = await flickr.PhotosGetSizesAsync(photo.PhotoId);
                            PreviewImage.UriSource = new Uri(sizes.OrderByDescending(size => Math.Abs(size.Width - thumbnailSize)).First().Url);
                            await flickr.DownloadFileAsync(folder, photo.Title, sizes, cancellationToken);
                            ProgressValue++;
                        }
                    }
                }

                ProgressValue = ProgressMax;
                CurrentOperationDescription = "Sync_FinishedStatus".GetLocalized();
            }
            catch (Exception)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        private async Task<StorageFolder> CreateFolderRecursivelyAsync(StorageFolder parent, string path, CancellationToken cancellationToken)
        {
            foreach (var folderName in path.Split(Path.DirectorySeparatorChar))
            {
                parent = await parent.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return parent;
        }
    }
}
