using FlickrNet;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Syncr.Helpers
{
    internal static class FlickrExtensions
    {
        internal static async Task<R> RetryOnFailureAsync<T, R>(this T instance, Func<T, Task<R>> func, int retryCount = 3, double delay = 100d)
        {
            int retries = 0;
            while (true)
            {
                try
                {
                    return await func(instance);
                }
                catch (Exception)
                {
                    retries++;
                    if (retries >= retryCount)
                    {
                        throw;
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(delay));
                    }
                }
            }
        }

        internal static async Task DownloadFileAsync(this Flickr flickr, StorageFolder folder, Photo photo, CancellationToken cancellationToken)
        {
            var sizes = await flickr.PhotosGetSizesAsync(photo.PhotoId);
            var url = sizes.OrderByDescending(s => s.Label == "Original" ? int.MaxValue : s.Width * s.Height).First().Source;
            var request = WebRequest.CreateHttp(url);

            using (cancellationToken.Register(request.Abort, false))
            using (var response = await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream())
            using (var outStream = await folder.OpenStreamForWriteAsync(photo.Title, CreationCollisionOption.ReplaceExisting))
            {
                await responseStream.CopyToAsync(outStream);
            }
        }
    }
}
