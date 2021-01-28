using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.UI.ApplicationSettings;

namespace App32
{
    class AuthProvider
    {
        public GraphServiceClient Graph { get; private set; }


        private string[] scopes;
        private string clientId;
        private string authority;

        public AuthProvider(string clientId, string[] scopes = null, string tenant = "common")
        {
            this.clientId = clientId;
            this.scopes = scopes ?? new string[] { "user.read" };//, "Calendars.Read" };
            this.authority = "https://login.microsoftonline.com/" + tenant;
            
            // this redirect URI is used in the AAD portal as redirect URI for native app
            // I got this to work by associating the app with the store, not sure if there is a way to avoid that
            string redirect_uri = string.Format("ms-appx-web://Microsoft.AAD.BrokerPlugIn/{0}", WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper());
            Debug.WriteLine(redirect_uri);

            AccountsSettingsPane.GetForCurrentView().AccountCommandsRequested += BuildPaneAsync;
            AccountsSettingsPane.Show();
        }

        private async void BuildPaneAsync(AccountsSettingsPane sender, AccountsSettingsPaneCommandsRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            var msaProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                "https://login.microsoft.com"); //providing nothing shows all accounts, providing authority shows only aad

            var command = new WebAccountProviderCommand(msaProvider, GetTokenAsync);

            args.WebAccountProviderCommands.Add(command);

            deferral.Complete();
        }

        private async void GetTokenAsync(WebAccountProviderCommand command)
        {
            WebTokenRequest request = new WebTokenRequest(command.WebAccountProvider, String.Join(',', this.scopes), clientId);
            request.Properties.Add("resource", "https://graph.microsoft.com");
            WebTokenRequestResult result = await WebAuthenticationCoreManager.RequestTokenAsync(request);

            var userAccount = result.ResponseData[0].WebAccount;
            var token = result.ResponseData[0].Token;

            var graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) => {
                requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("Bearer", token);

                return Task.FromResult(0);
            }));

            var graphResponse = await graphServiceClient
                .Me
                .Request()
                .GetAsync();
        }

        private async Task init()
        {
            //this.publicClientApp = PublicClientApplicationBuilder.Create(this.clientId)
            //    .WithAuthority(this.authority)
            //    .WithUseCorporateNetwork(false)
            //    .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
            //    .Build();

            //IEnumerable<IAccount> accounts = await this.publicClientApp.GetAccountsAsync().ConfigureAwait(false);
            //IAccount firstAccount = accounts.FirstOrDefault();

            //AuthenticationResult authResult;

            //try
            //{
            //    authResult = await this.publicClientApp.AcquireTokenSilent(this.scopes, firstAccount)
            //                                      .ExecuteAsync();
            //}
            //catch (MsalUiRequiredException ex)
            //{
            //    // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
            //    Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

            //    authResult = await this.publicClientApp.AcquireTokenInteractive(scopes)
            //                                      .ExecuteAsync()
            //                                      .ConfigureAwait(false);

            //}
            //Debug.WriteLine(authResult.AccessToken);

            

            //var wap = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", authority);
            ////WebTokenRequest wtr = new WebTokenRequest(wap, string.Empty, clientId);
            ////wtr.Properties.Add("resource", "https://graph.microsoft.com");

            ////WebTokenRequestResult wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);

            //WebTokenRequest wtr = new WebTokenRequest(wap, string.Empty, clientId, WebTokenRequestPromptType.ForceAuthentication);
            //wtr.Properties.Add("resource", "https://graph.microsoft.com");
            //WebTokenRequestResult wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);

            //var userAccount = wtrr.ResponseData[0].WebAccount;

        }
    }
}
