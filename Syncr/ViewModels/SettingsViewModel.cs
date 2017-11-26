using System;
using System.Windows.Input;

using Syncr.Helpers;
using Syncr.Services;

using Windows.ApplicationModel;
using Windows.UI.Xaml;
using System.Linq;
using System.Collections.Generic;
using static Syncr.Services.SettingsService;

namespace Syncr.ViewModels
{
    public class SettingsViewModel : Observable
    {
        // TODO WTS: Add other settings as necessary. For help see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/pages/settings.md
        private ElementTheme _elementTheme = ThemeSelectorService.Theme;

        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { Set(ref _elementTheme, value); }
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { Set(ref _versionDescription, value); }
        }

        private ICommand _switchThemeCommand;

        public ICommand SwitchThemeCommand
        {
            get
            {
                if (_switchThemeCommand == null)
                {
                    _switchThemeCommand = new RelayCommand<ElementTheme>(
                        async (param) =>
                        {
                            ElementTheme = param;
                            await ThemeSelectorService.SetThemeAsync(param);
                        });
                }

                return _switchThemeCommand;
            }
        }

        public IEnumerable<string> Modes => SettingsService.Modes.Select(mode => mode.ToString());

        public string SelectedMode
        {
            get
            {
                return Singleton<SettingsService>.Instance.Mode.ToString();
            }

            set
            {
                if (Enum.TryParse(value, out FunctionMode functionMode) && !functionMode.Equals(Singleton<SettingsService>.Instance.Mode))
                {
                    Singleton<SettingsService>.Instance.Mode = functionMode;
                    OnPropertyChanged(nameof(SelectedMode));
                }
            }
        }

        public SettingsViewModel()
        {
        }

        public void Initialize()
        {
            VersionDescription = GetVersionDescription();
        }

        private string GetVersionDescription()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{package.DisplayName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
