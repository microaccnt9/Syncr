using System;

using Syncr.Helpers;
using Syncr.Services;
using Windows.UI.Xaml.Media.Imaging;
using System.Windows.Input;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Syncr.Views;

namespace Syncr.ViewModels
{
    public class MainViewModel : Observable
    {
        private FlickrService flickrService;

        internal FlickrService Flickr
        {
            get { return flickrService; }
            set { Set(ref flickrService, value); }
        }

        private string userName;

        public string UserName
        {
            get { return userName; }
            set { Set(ref userName, value); }
        }

        private BitmapImage icon;

        public BitmapImage Icon
        {
            get { return icon; }
            set { Set(ref icon, value); }
        }

        private StorageFolder syncFolder;

        public StorageFolder SyncFolder
        {
            get { return syncFolder; }
            set
            {
                ApplicationData.Current.LocalSettings.Values["syncFolder"] = value.Path;
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("syncFolder", value);
                Set(ref syncFolder, value);
                SyncFolderPath = value.Path;
                OnPropertyChanged(nameof(SyncFolderPath));
                OnPropertyChanged(nameof(SyncCommand));
            }
        }

        public string SyncFolderPath { get; private set; }

        private ICommand folderSelection;

        public ICommand FolderSelection
        {
            get
            {
                if (folderSelection == null)
                {
                    folderSelection = new RelayCommand(SelectFolderAsync);
                }
                return folderSelection;
            }
        }

        private async void SelectFolderAsync()
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail,
                SettingsIdentifier = "syncFolderPicker"
            };
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                SyncFolder = folder;
            }
        }

        private ICommand syncCommand;

        public ICommand SyncCommand
        {
            get
            {
                if (syncCommand == null)
                {
                    syncCommand = new RelayCommand(StartSync, () => SyncFolder != null);
                }
                return syncCommand;
            }
        }

        private void StartSync()
        {
            NavigationService.Navigate<SyncPage>(SyncFolder);
        }

        public MainViewModel()
        {
            flickrService = Singleton<FlickrService>.Instance;
            Icon = new BitmapImage(flickrService.IconUri);
            UserName = flickrService.UserName;
            if (syncFolder == null)
            {
                var path = ApplicationData.Current.LocalSettings.Values["syncFolder"];
                if (path != null)
                {
                    StorageFolder.GetFolderFromPathAsync(path.ToString()).AsTask().ContinueWith(t => SyncFolder = t.Result);
                }
            }
        }
    }
}
