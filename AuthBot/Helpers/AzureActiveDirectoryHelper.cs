// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace AuthBot.Helpers
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using System.Net.Http;
    using System.Text;
    using System.Net.Http.Headers;
    using System.Collections.Generic;
    using Models;
    public static class AzureActiveDirectoryHelper
    {
      

        public static async Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie)
        {
            var encodedCookie = UrlToken.Encode(resumptionCookie);

            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);
            if (string.Equals(AuthSettings.Mode, "v1",StringComparison.OrdinalIgnoreCase))
            {
                Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(AuthSettings.EndpointUrl + "/" + AuthSettings.Tenant);

                var uri = await context.GetAuthorizationRequestUrlAsync(
                    AuthSettings.ResourceId,
                    AuthSettings.ClientId,
                    redirectUri,
                    Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier.AnyUser,
                    "state=" + encodedCookie);

                return uri.ToString();
            }
            else if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
            {

                InMemoryTokenCacheMSAL tokenCache = new InMemoryTokenCacheMSAL();

                Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(AuthSettings.ClientId,redirectUri.ToString(),
                    new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret), 
                    tokenCache);

                var uri = await client.GetAuthorizationRequestUrlAsync(
                    AuthSettings.Scopes,
                    null,
                    "state=" + encodedCookie);
                //,
                //    null
                //    clientId.Value,
                //    redirectUri,
                //    Microsoft.Experimental.IdentityModel.Clients.ActiveDirectory.UserIdentifier.AnyUser,
                //    "state=" + encodedCookie);

                return uri.ToString();
            }
            else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return null;
        }

        public static async Task<AuthResult> GetTokenByAuthCodeAsync(string authorizationCode, Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache tokenCache)
        {
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(AuthSettings.EndpointUrl + "/" + AuthSettings.Tenant, tokenCache);

            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);

            var result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(AuthSettings.ClientId, AuthSettings.ClientSecret));

            Trace.TraceInformation("Token Cache Count:" + context.TokenCache.Count);

            AuthResult authResult = AuthResult.FromADALAuthenticationResult(result, tokenCache);
            return authResult;
        }
        public static async Task<AuthResult> GetTokenByAuthCodeAsync(string authorizationCode, Microsoft.Identity.Client.TokenCache tokenCache)
        {
            Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(AuthSettings.ClientId, AuthSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret), tokenCache);
            
            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);
           
            //Temporary workaround since the current experimental library is unable to decode the token in v2
            //var result2 = await GetTokenFromAuthCodeAsyncV2(activeDirectoryEndpointUrl.Value, activeDirectoryTenant.Value, scopes.Value, clientId.Value, clientSecret.Value, authorizationCode, redirectUri.ToString());
            
            var result = await client.AcquireTokenByAuthorizationCodeAsync(AuthSettings.Scopes, authorizationCode);

            AuthResult authResult = AuthResult.FromMSALAuthenticationResult(result, tokenCache);
          

            return authResult;
        }

        public static async Task<AuthResult> GetToken(string userUniqueId, Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache tokenCache)
        {
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(AuthSettings.EndpointUrl + "/" + AuthSettings.Tenant, tokenCache);

            var result = await context.AcquireTokenSilentAsync(AuthSettings.ResourceId, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(AuthSettings.ClientId, AuthSettings.ClientSecret), new Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier(userUniqueId, Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifierType.UniqueId));

            AuthResult authResult = AuthResult.FromADALAuthenticationResult(result, tokenCache);
            return authResult;
        }

        public static async Task<AuthResult> GetToken(string userUniqueId, Microsoft.Identity.Client.TokenCache tokenCache)
        {
            Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(AuthSettings.ClientId, AuthSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret), tokenCache);
            var result = await client.AcquireTokenSilentAsync(AuthSettings.Scopes, userUniqueId);
            AuthResult authResult = AuthResult.FromMSALAuthenticationResult(result, tokenCache);
            return authResult;
        }

        private static async Task<string> GetTokenFromAuthCodeAsyncV2(string authorizationCode)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(AuthSettings.EndpointUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("scope", string.Join(" ",AuthSettings.Scopes)+" openid offline_access"),
                    new KeyValuePair<string, string>("client_id", AuthSettings.ClientId),
                    new KeyValuePair<string, string>("client_secret", AuthSettings.ClientSecret),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", authorizationCode),
                    new KeyValuePair<string, string>("redirect_uri", AuthSettings.RedirectUrl),
                 });
                var result = await client.PostAsync("/"+ AuthSettings.Tenant + "/oauth2/v2.0/token", content);
                string resultContent = await result.Content.ReadAsStringAsync();
                return resultContent;
            }

        }

    }
}

//*********************************************************
//
//AuthBot, https://github.com/matvelloso/AuthBot
//
//Copyright (c) Microsoft Corporation
//All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// ""Software""), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:




// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.




// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//*********************************************************
