using Syncr.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Windows.Storage;

namespace Syncr.Services
{
    public class SettingsService : Observable
    {
        public enum FunctionMode
        {
            [Display(Name = "Upload")]
            UploadOnly,
            [Display(Name = "Download")]
            DownloadOnly,
            [Display(Name = "Synchronize")]
            Sync
        }

        private FunctionMode? mode;

        public FunctionMode Mode
        {
            get
            {
                if (!mode.HasValue)
                {
                    var setting = ApplicationData.Current.LocalSettings.Values[nameof(Mode)];
                    if (setting == null || !Enum.TryParse(setting.ToString(), out FunctionMode modeValue))
                    {
                        modeValue = FunctionMode.Sync;
                        ApplicationData.Current.LocalSettings.Values[nameof(Mode)] = modeValue.ToString();
                    }
                    mode = modeValue;
                }
                return mode.Value;
            }

            set
            {
                ApplicationData.Current.LocalSettings.Values[nameof(Mode)] = value.ToString();
                Set(ref mode, value);
            }
        }

        public static IEnumerable<FunctionMode> Modes => Enum.GetValues(typeof(FunctionMode)).Cast<FunctionMode>();
    }
}
