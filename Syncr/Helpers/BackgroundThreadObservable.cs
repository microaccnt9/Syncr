using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Syncr.Helpers
{
    public class BackgroundThreadObservable : Observable
    {
        protected Task OnUiThread(Action action)
            => CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(action))
            .AsTask();

        protected override void OnPropertyChanged(string propertyName) => OnUiThread(() => base.OnPropertyChanged(propertyName));
    }
}
