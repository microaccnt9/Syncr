using System;

using Syncr.ViewModels;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

namespace Syncr.Views
{
    public sealed partial class SyncPage : Page
    {
        public SyncViewModel ViewModel { get; } = new SyncViewModel();

        public SyncPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SyncFolder = (IStorageFolder)e.Parameter;
        }
    }
}
