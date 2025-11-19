using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dropbox.Api;
using PneumaticTube.Properties;

namespace PneumaticTube
{
    internal static class DropboxClientFactory
    {
		const string _appkey = "ii29ofre0mjt9vf";

		internal static void ResetAuthentication()
		{
			Settings.Default.ACCESS_TOKEN = string.Empty;
			Settings.Default.REFRESH_TOKEN = string.Empty;
			Settings.Default.TOKEN_EXPIRATION = string.Empty;
			Settings.Default.Save();
		}

		public static async Task<DropboxClient> CreateDropboxClient(int timeoutSeconds)
        {
	        var result = await GetAccessTokens();

            var config = new DropboxClientConfig("PneumaticTube/2")
            {
                HttpClient = new System.Net.Http.HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                }
            };

			
			// TODO Make sure that -r works even without any other commands (This might be challenging with the command line options stuff)
			
			if(result.ExpiresAt.HasValue)
			{
				return new DropboxClient(oauth2AccessToken: result.AccessToken, oauth2RefreshToken: result.RefreshToken, 
					oauth2AccessTokenExpiresAt: result.ExpiresAt.Value, appKey: _appkey, config: config);
			}

			return new DropboxClient(oauth2RefreshToken: result.RefreshToken, appKey: _appkey, config: config);
        }

	    private static async Task<TokenResult> GetAccessTokens()
	    {
			if (!string.IsNullOrEmpty(Settings.Default.ACCESS_TOKEN) && !string.IsNullOrEmpty(Settings.Default.REFRESH_TOKEN))
		    {
			    return new TokenResult(Settings.Default.ACCESS_TOKEN, Settings.Default.REFRESH_TOKEN, null);
		    }

		    Console.WriteLine(
			    "You'll need to authorize this account with PneumaticTube; a browser window will now open asking you to log into Dropbox and allow the app. When you've done that, you'll be given an access key. Enter the key here and hit Enter:");

			var oauthFlow = new PKCEOAuthFlow();

		    // Pop open the authorization page in the default browser
		    var url = oauthFlow.GetAuthorizeUri(OAuthResponseType.Code, _appkey, tokenAccessType: TokenAccessType.Offline);
		    Process.Start(url.ToString());

		    // Wait for the user to enter the code
		    var code = Console.ReadLine();

			var response = await oauthFlow.ProcessCodeFlowAsync(code, _appkey);

			// Save the token 
			SaveTokens(response);

		    return new TokenResult(response.AccessToken, response.RefreshToken, response.ExpiresAt);
	    }

		private static void SaveTokens(OAuth2Response response)
		{
			Settings.Default.ACCESS_TOKEN = response.AccessToken;
			Settings.Default.REFRESH_TOKEN = response.RefreshToken;
			Settings.Default.TOKEN_EXPIRATION = response.ExpiresAt.ToString() ?? string.Empty;
			Settings.Default.Save();
		}

		// TODO Make this a record 
		struct TokenResult 
		{
			public string AccessToken;
			public string RefreshToken;
			public DateTime? ExpiresAt;

            public TokenResult(string accessToken, string refreshToken, DateTime? expiresAt)
            {
                AccessToken = accessToken;
                RefreshToken = refreshToken;
				ExpiresAt = expiresAt;
            }
        }
    }
}