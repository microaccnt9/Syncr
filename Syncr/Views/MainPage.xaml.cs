using System;

using Syncr.ViewModels;

using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
using Syncr.Services;

namespace Syncr.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            NavigationService.ClearBackStack();
        }
    }
}
