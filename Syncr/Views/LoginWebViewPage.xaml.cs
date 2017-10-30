using System;

using Syncr.ViewModels;

using Windows.UI.Xaml.Controls;

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
    }
}
