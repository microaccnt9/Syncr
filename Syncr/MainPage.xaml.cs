using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using FlickrNet;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Syncr
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// Photo formats
    ///JPEG.
    ///PNG.
    ///GIF (non-animated).
    ///All other formats will be converted to JPEG.
    ///Video formats
    ///MP4 (recommended with H.264)
    ///AVI(Proprietary codecs may not work)
    ///WMV
    ///MOV(AVID or other proprietary codecs may not work)
    ///MPEG(1, 2, and 4)
    ///3gp
    ///M2TS
    ///OGG
    ///OGV
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFolder syncFolder;
        private readonly Flickr flickrNet;
        private OAuthRequestToken requestToken;
        private OAuthAccessToken accessToken;

        public MainPage()
        {
            this.InitializeComponent();
            FolderButton.Click += FolderButton_Click;
            SyncButton.Click += SyncButton_Click;
            LoginButton.Click += LoginButton_ClickAsync;
            flickrNet = new Flickr("5cc521a6a8599edbd471fa9c59c30260", "13fdeeb5eb5539d8");
        }

        private async void LoginButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            if (AuthText.Visibility == Visibility.Collapsed)
            {
                requestToken = await flickrNet.OAuthRequestTokenAsync("oob");
                var url = flickrNet.OAuthCalculateAuthorizationUrl(requestToken.Token, AuthLevel.Write);
                await Launcher.LaunchUriAsync(new Uri(url));
                AuthText.Visibility = Visibility.Visible;
            }
            else
            {
                accessToken = await flickrNet.OAuthAccessTokenAsync(requestToken.Token, requestToken.TokenSecret, AuthText.Text);
                flickrNet.OAuthAccessToken = accessToken.Token;
                flickrNet.OAuthAccessTokenSecret = accessToken.TokenSecret;
                AuthText.Visibility = Visibility.Collapsed;
                LoginButton.Visibility = Visibility.Collapsed;
                var user = await flickrNet.TestLoginAsync();
                UserNameText.Text = user.UserName;
                UserNameText.Visibility = Visibility.Visible;
                var info = await flickrNet.PeopleGetInfoAsync(user.UserId);
                var iconUri = new Uri(int.TryParse(info.IconServer, out int iconServer) && iconServer > 0
                    ? $"http://farm{info.IconFarm}.staticflickr.com/{iconServer}/buddyicons/{user.UserId}.jpg"
                    : "https://www.flickr.com/images/buddyicon.gif");
                UserIconImage.Source = new BitmapImage(iconUri);
                UserIconImage.Visibility = Visibility.Visible;
            }
            LoginButton.IsEnabled = true;
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail,
                SettingsIdentifier = "syncFolder"
            };
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (syncFolder != null)
            {
                syncFolder = folder;
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("OutputFolder", syncFolder);
                FolderText.Text = syncFolder.Path;
            }
        }
    }
}
