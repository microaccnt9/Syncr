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

namespace Syncr.ViewModels
{
    public class SyncViewModel : Observable
    {
        private string[] extensions = { ".jpeg", ".jpg", ".png", ".gif", ".mp4", ".avi", ".wmv", ".mov", ".mpeg", ".mpg", ".m2v", ".3gp", ".m2ts", ".ogg", ".ogv" };
        private IStorageFolder syncFolder;

        public IStorageFolder SyncFolder
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
        }

        public SyncViewModel()
        {
            ProgressValue = 0.4;
            ProcessingTask = Task.Factory.StartNew(SynchronizeAsync);
//            ProcessingTask.ContinueWith(t => NavigationService.GoBack()); //TODO: on UI thread
        }

        private async void SynchronizeAsync()
        {
            var flickr = Singleton<FlickrService>.Instance.FlickrNet;
            var files = new List<IStorageFile>();
            var folderQueue = new Queue<IStorageFolder>(new[] { syncFolder });
            while (folderQueue.Count > 0)
            {
                var folder = folderQueue.Dequeue();
                var items = await folder.GetItemsAsync();
                foreach (var item in items)
                {
                    var photoset = await flickr.PhotosetsCreateAsync(folder.Name, folder.Path);
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        folderQueue.Enqueue((IStorageFolder)item);
                    }
                    else if (item.IsOfType(StorageItemTypes.File) && extensions.Any(ext => item.Name.EndsWith(ext, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        using (var stream = (await ((IStorageFile)item).OpenSequentialReadAsync()).AsStreamForRead())
                        {
                            var result = await flickr.UploadPictureAsync(stream, item.Name, item.Name, item.Path, "", false, false, false, FlickrNet.ContentType.Photo, FlickrNet.SafetyLevel.None, FlickrNet.HiddenFromSearch.Hidden);
                            await flickr.PhotosetsAddPhotoAsync(photoset.PhotosetId, result);
                        }
                    }
                }
            }
        }
    }
}
