namespace AuthBot
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Models;
    using System.Configuration;
    public static class ContextExtensions
    {
       

        public static async Task<string> GetAccessToken(this IBotContext context)
        {
            AuthResult authResult;

            if (context.UserData.TryGetValue(ContextConstants.AuthResultKey, out authResult))
            {
                DateTime expires = new DateTime(authResult.ExpiresOnUtcTicks);

                if (DateTime.UtcNow >= expires)
                {
                    Trace.TraceInformation("Token Expired");

                    try
                    {
                        if (string.Equals(AuthSettings.Mode, "v1", StringComparison.OrdinalIgnoreCase))
                        {
                            InMemoryTokenCacheADAL tokenCache = new InMemoryTokenCacheADAL(authResult.TokenCache);

                            Trace.TraceInformation("Trying to renew token...");
                            var result = await AzureActiveDirectoryHelper.GetToken(authResult.UserUniqueId, tokenCache);

                            authResult.AccessToken = result.AccessToken;
                            authResult.ExpiresOnUtcTicks = result.ExpiresOnUtcTicks;
                            authResult.TokenCache = tokenCache.Serialize();

                            context.StoreAuthResult(authResult);
                        }
                        else if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
                        {
                            InMemoryTokenCacheMSAL tokenCache = new InMemoryTokenCacheMSAL(authResult.TokenCache);

                            Trace.TraceInformation("Trying to renew token...");
                            var result = await AzureActiveDirectoryHelper.GetToken(authResult.UserUniqueId, tokenCache);

                            authResult.AccessToken = result.AccessToken;
                            authResult.ExpiresOnUtcTicks = result.ExpiresOnUtcTicks;
                            authResult.TokenCache = tokenCache.Serialize();

                            context.StoreAuthResult(authResult);
                        }
                        else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                            Trace.TraceInformation("Token renewed!");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Failed to renew token: " + ex.Message);

                        await context.PostAsync("Your credentials expired and could not be renewed automatically!");
                        context.Logout();

                        return null;
                    }
                }

                return authResult.AccessToken;
            }

            return null;
        }

        public static void StoreAuthResult(this IBotContext context, AuthResult authResult)
        {
            context.UserData.SetValue(ContextConstants.AuthResultKey, authResult);
        }

        public static async Task Logout(this IBotContext context)
        {
            context.UserData.RemoveValue(ContextConstants.AuthResultKey);
            context.UserData.RemoveValue(ContextConstants.MagicNumberKey);
            context.UserData.RemoveValue(ContextConstants.MagicNumberValidated);
            string signoutURl = "https://login.microsoftonline.com/common/oauth2/logout?post_logout_redirect_uri=" + System.Net.WebUtility.UrlEncode(AuthSettings.RedirectUrl);

            await context.PostAsync($"In order to finish the sign out, please click at this [link]({signoutURl}).");

        }

    }
}
