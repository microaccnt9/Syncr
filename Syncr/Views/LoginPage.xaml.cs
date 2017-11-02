using System;

using Windows.UI.Xaml.Controls;
using Syncr.Services;
using Syncr.Helpers;
using Windows.UI.Xaml;

namespace Syncr.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void Page_LoadedAsync(object sender, RoutedEventArgs e)
        {
            await Singleton<FlickrService>.Instance.InitialiseAsync();
            if (Singleton<FlickrService>.Instance.IsAuthenticated)
            {
                NavigationService.Navigate<PivotPage>();
            }
            else
            {
                NavigationService.Navigate<LoginWebViewPage>(await Singleton<FlickrService>.Instance.GetAuthPageUrlAsync());
            }
        }
    }
}
