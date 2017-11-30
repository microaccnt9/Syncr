using System;

using Syncr.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Syncr.Views
{
    public sealed partial class LicensePage : Page
    {
        public LicenseViewModel ViewModel { get; } = new LicenseViewModel();

        public LicensePage()
        {
            InitializeComponent();
        }
    }
}
