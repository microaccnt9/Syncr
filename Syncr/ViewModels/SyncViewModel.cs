using System;

using Syncr.Helpers;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.Storage;
using System.Threading;
using System.Threading.Tasks;
using Syncr.Services;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Windows.Storage.Search;
using FlickrNet;

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

            CurrentOperationDescription = $"Parsing folder \"{SyncFolder.Path}\"";
            var queryResult = SyncFolder.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.DefaultQuery, extensions));
            var files = await queryResult.GetFilesAsync();
            ProgressMax = files.Count;

            var groupedFiles = files.GroupBy(f => Path.GetDirectoryName(f.Path));

            foreach (var group in groupedFiles)
            {
                Photoset photoset = null;
                var photosetName = Path.GetFileName(group.Key);
                foreach (var file in group)
                {
                    CurrentOperationDescription = $"Uploading file \"{file.Path}\"";
                    string photoId = null;
                    using (var stream = (await file.OpenSequentialReadAsync()).AsStreamForRead())
                    {
                        photoId = await flickr.UploadPictureAsync(stream, file.Name, file.Name, file.Path, "", false, false, false, FlickrNet.ContentType.Photo, FlickrNet.SafetyLevel.None, FlickrNet.HiddenFromSearch.Hidden);
                    }

                    if (photoset == null)
                    {
                        CurrentOperationDescription = $"Creating photoset \"{photosetName}\"";
                        photoset = await flickr.PhotosetsCreateAsync(photosetName, "", photoId);
                    }
                    else
                    {
                        CurrentOperationDescription = $"Adding file \"{file.Name}\" to photoset \"{photosetName}\"";
                        await flickr.PhotosetsAddPhotoAsync(photoset.PhotosetId, photoId);
                    }

                    ProgressValue++;
                }
            }
        }
    }
}
