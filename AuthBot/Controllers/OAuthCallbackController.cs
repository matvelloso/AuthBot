namespace AuthBot.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Autofac;
    using Helpers;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Bot.Connector;
    using Models;
    using System.Configuration;
    public class OAuthCallbackController : ApiController
    {
       

       

      
        [HttpGet]
        [Route("api/OAuthCallback")]
        public async Task<HttpResponseMessage> OAuthCallback([FromUri] string code, [FromUri] string state)
        {
            try
            {
                object tokenCache = null;
                if (string.Equals(AuthSettings.Mode, "v1", StringComparison.OrdinalIgnoreCase))
                {
                    tokenCache = new Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache();
                }
                else if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
                {
                    tokenCache = new Microsoft.Identity.Client.TokenCache();
                }
                else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
                {
                }

                // Get the resumption cookie
                var resumptionCookie = UrlToken.Decode<ResumptionCookie>(state);
                // Create the message that is send to conversation to resume the login flow
                var message = resumptionCookie.GetMessage();
               
                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    var client = scope.Resolve<IConnectorClient>();
                   

                    AuthResult authResult = null;

                    if (string.Equals(AuthSettings.Mode, "v1", StringComparison.OrdinalIgnoreCase))
                    {

                        // Exchange the Auth code with Access token
                        var token = await AzureActiveDirectoryHelper.GetTokenByAuthCodeAsync(code, (Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache)tokenCache);

                        authResult = token;
                    }
                    else if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
                    {

                        // Exchange the Auth code with Access token
                        var token = await AzureActiveDirectoryHelper.GetTokenByAuthCodeAsync(code, (Microsoft.Identity.Client.TokenCache)tokenCache);

                        authResult = token;
                    }
                    else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
                    {
                    }


                    var data = await client.Bots.GetPerUserConversationDataAsync(resumptionCookie.BotId, resumptionCookie.ConversationId, resumptionCookie.UserId);

                    data.SetProperty(ContextConstants.AuthResultKey, authResult);
                    int magicNumber = GenerateRandomNumber();
                    data.SetProperty(ContextConstants.MagicNumberKey, magicNumber);
                    data.SetProperty(ContextConstants.MagicNumberValidated, "false");

                    await client.Bots.SetUserDataAsync(resumptionCookie.BotId, resumptionCookie.UserId, data);

                    var reply = await Conversation.ResumeAsync(resumptionCookie, message);

                    reply.To = message.From;
                    reply.From = message.To;

                    await client.Messages.SendMessageAsync(reply);


                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>Almost done! Please copy this number and paste it back to your chat so your authentication can complete: {magicNumber}.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;
                }

             

               
            }
            catch (Exception ex)
            {
                // Callback is called with no pending message as a result the login flow cannot be resumed.
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, new InvalidOperationException("Cannot resume!"));
            }
        }

        private int GenerateRandomNumber()
        {
            Random rnd = new Random();
            return rnd.Next(10000, 100000);
        }
    }
}
