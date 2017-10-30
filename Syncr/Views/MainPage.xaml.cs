using System;

using Syncr.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Syncr.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
