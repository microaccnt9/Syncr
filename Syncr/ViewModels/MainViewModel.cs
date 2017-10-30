using System;

using Syncr.Helpers;
using Syncr.Services;

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


        public MainViewModel()
        {
            flickrService = new FlickrService();
        }
    }
}
