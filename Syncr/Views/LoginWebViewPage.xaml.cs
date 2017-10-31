using System;

using Syncr.ViewModels;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Syncr.Views
{
    public sealed partial class LoginWebViewPage : Page
    {
        public LoginWebViewViewModel ViewModel { get; } = new LoginWebViewViewModel();

        public LoginWebViewPage()
        {
            InitializeComponent();
            ViewModel.Initialize(webView);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.NavigateTo(e.Parameter.ToString());
        }
    }
}
