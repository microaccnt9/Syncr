using System;

using Syncr.Helpers;
using Syncr.Services;
using Windows.UI.Xaml.Media.Imaging;

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

        public MainViewModel()
        {
            flickrService = Singleton<FlickrService>.Instance;
            Icon = new BitmapImage(flickrService.IconUri);
            UserName = flickrService.UserName;
        }
    }
}
