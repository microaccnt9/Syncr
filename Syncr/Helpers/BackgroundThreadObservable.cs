using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Syncr.Helpers
{
    public class BackgroundThreadObservable : Observable
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        protected void OnUiThread(Action action) => CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(action));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        protected override void OnPropertyChanged(string propertyName) => OnUiThread(() => base.OnPropertyChanged(propertyName));
    }
}
