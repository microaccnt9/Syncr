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
    public class FlickrService : Observable
    {
        public const string DefaultCallbackUrl = @"ms-appx-web:///assets/authenticated.html";

        private bool isAuthenticated;

        public bool IsAuthenticated
        {
            get { return isAuthenticated; }
            set { Set(ref isAuthenticated, value); }
        }

        public Flickr FlickrNet { get; }

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

        private string userName;

        public string UserName
        {
            get { return userName; }
            set { Set(ref userName, value); }
        }

        private Uri iconUri;

        public Uri IconUri
        {
            get { return iconUri; }
            set { Set(ref iconUri, value); }
        }

        public FlickrService()
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
        }

        internal async Task InitialiseAsync()
        {
            await CheckLogin();
        }

        public async Task<string> GetAuthPageUrlAsync(string callbackUrl = DefaultCallbackUrl)
        {
            RequestToken = await FlickrNet.RetryOnFailureAsync(f => f.OAuthRequestTokenAsync(callbackUrl));
            return FlickrNet.OAuthCalculateAuthorizationUrl(RequestToken.Token, AuthLevel.Write);
        }

        public async Task<bool> AuthenticateAsync(string validation)
        {
            var accessToken = await FlickrNet.RetryOnFailureAsync(f => f.OAuthAccessTokenAsync(requestToken.Token, requestToken.TokenSecret, validation));
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
            var user = await FlickrNet.RetryOnFailureAsync(f => f.TestLoginAsync());
            if (user != null)
            {
                UserName = user.UserName;
                var info = await FlickrNet.RetryOnFailureAsync(f => f.PeopleGetInfoAsync(user.UserId));
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
