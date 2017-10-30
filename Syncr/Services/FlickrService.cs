using FlickrNet;
using Syncr.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Syncr.Services
{
    internal class FlickrService : Observable
    {
        private bool isAuthenticated;

        internal bool IsAuthenticated
        {
            get { return isAuthenticated; }
            set { Set(ref isAuthenticated, value); }
        }

        internal Flickr FlickrNet { get; }

        private OAuthRequestToken requestToken;

        private OAuthRequestToken RequestToken
        {
            get { return requestToken; }
            set
            {
                ApplicationData.Current.LocalSettings.Values["requestToken"] = value == null ? null : value.Token + "|" + value.TokenSecret;
                Set(ref requestToken, value);
            }
        }

        public string UserName { get; private set; }
        public Uri IconUri { get; private set; }

        internal FlickrService()
        {
            FlickrNet = new Flickr("5cc521a6a8599edbd471fa9c59c30260", "13fdeeb5eb5539d8");
            var localSettings = ApplicationData.Current.LocalSettings;
            var accessToken = localSettings.Values["accessToken"];
            if (accessToken != null)
            {
                FlickrNet.OAuthAccessToken = accessToken.ToString();
            }
            var tokenSecret = localSettings.Values["tokenSecret"];
            if (tokenSecret != null)
            {
                FlickrNet.OAuthAccessTokenSecret = tokenSecret.ToString();
            }
            var requestTokens = localSettings.Values["requestToken"];
            if (requestTokens != null)
            {
                var oAuthRequestToken = requestTokens.ToString().Split('|');
                requestToken = new OAuthRequestToken { Token = oAuthRequestToken[0], TokenSecret = oAuthRequestToken[1] };
            }
            CheckLogin();
        }

        internal async Task<Uri> GetAuthPageUriAsync(string callbackUrl = "syncr:syncrapp")
        {
            RequestToken = await FlickrNet.OAuthRequestTokenAsync(callbackUrl);
            return new Uri(FlickrNet.OAuthCalculateAuthorizationUrl(RequestToken.Token, AuthLevel.Write));
        }

        internal async Task<bool> AuthenticateAsync(string validation)
        {
            var accessToken = await FlickrNet.OAuthAccessTokenAsync(requestToken.Token, requestToken.TokenSecret, validation);
            FlickrNet.OAuthAccessToken = accessToken.Token;
            FlickrNet.OAuthAccessTokenSecret = accessToken.TokenSecret;
            if (await CheckLogin())
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["accessToken"] = accessToken.Token;
                localSettings.Values["tokenSecret"] = accessToken.TokenSecret;
                RequestToken = null;
            }
            return IsAuthenticated;
        }

        private async Task<bool> CheckLogin()
        {
            if (FlickrNet == null || FlickrNet.OAuthAccessToken == null || FlickrNet.OAuthAccessTokenSecret == null)
            {
                IsAuthenticated = false;
                return IsAuthenticated;
            }
            var user = await FlickrNet.TestLoginAsync();
            if (user != null)
            {
                UserName = user.UserName;
                var info = await FlickrNet.PeopleGetInfoAsync(user.UserId);
                IconUri = new Uri(int.TryParse(info.IconServer, out int iconServer) && iconServer > 0
                    ? $"http://farm{info.IconFarm}.staticflickr.com/{iconServer}/buddyicons/{user.UserId}.jpg"
                    : "https://www.flickr.com/images/buddyicon.gif");
                IsAuthenticated = true;
            }
            else
            {
                IsAuthenticated = false;
            }
            return IsAuthenticated;
        }
    }
}
