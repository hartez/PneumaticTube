using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dropbox.Api;
using PneumaticTube.Properties;

namespace PneumaticTube
{
    internal static class DropboxClientFactory
    {
		internal static void ResetAuthentication()
		{
			Settings.Default.USER_SECRET = string.Empty;
			Settings.Default.USER_TOKEN = string.Empty;
			Settings.Default.Save();
		}

		public static async Task<DropboxClient> CreateDropboxClient()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using(var stream = assembly.GetManifestResourceStream("PneumaticTube.apikeys.txt"))
            {
                using(var textStreamReader = new StreamReader(stream))
                {
                    var key = textStreamReader.ReadLine().Trim();
                    var secret = textStreamReader.ReadLine().Trim();

	                string accessToken = await GetAccessToken(key, secret);

                    return new DropboxClient(accessToken, new DropboxClientConfig("PneumaticTube/2"));
                }
            }
        }

	    private static async Task<string> GetAccessToken(string key, string secret)
	    {
		    if (!string.IsNullOrEmpty(Settings.Default.USER_TOKEN))
		    {
			    return Settings.Default.USER_TOKEN;
		    }

		    Console.WriteLine(
			    "You'll need to authorize this account with PneumaticTube; a browser window will now open asking you to log into Dropbox and allow the app. When you've done that, you'll be given an access key. Enter the key here and hit Enter:");

		    var oauth2State = Guid.NewGuid().ToString("N");

		    // Pop open the authorization page in the default browser
		    var url = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, key, (Uri) null, oauth2State);
		    Process.Start(url.ToString());

		    // Wait for the user to enter the key
		    var token = Console.ReadLine();

			var response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(token, key, secret);

			// Save the token 
			Settings.Default.USER_TOKEN = response.AccessToken;
		    Settings.Default.Save();

		    return response.AccessToken;
	    }
    }
}