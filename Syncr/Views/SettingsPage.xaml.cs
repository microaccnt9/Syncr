using System;

using Syncr.ViewModels;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Syncr.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

        //// TODO WTS: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere

        public SettingsPage()
        {
            InitializeComponent();
            ViewModel.Initialize();
        }
    }
}
