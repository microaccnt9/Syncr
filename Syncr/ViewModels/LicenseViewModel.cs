using System;

using Syncr.Helpers;
using Windows.Storage;
using Windows.ApplicationModel;

namespace Syncr.ViewModels
{
    public class LicenseViewModel : BackgroundThreadObservable
    {
        public LicenseViewModel()
        {
            ReadLicense();
        }

        private string licenseText;

        public string LicenseText
        {
            get { return licenseText; }
            set { Set(ref licenseText, value); }
        }

        private async void ReadLicense()
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync(@"Assets\LICENSE");
            LicenseText = await FileIO.ReadTextAsync(file);
        }
    }
}
