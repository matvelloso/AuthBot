// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
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
                        //TODO: Scopes definition here
                        // Exchange the Auth code with Access token
                        var token = await AzureActiveDirectoryHelper.GetTokenByAuthCodeAsync(code, (Microsoft.Identity.Client.TokenCache)tokenCache,new string[] { "User.Read" });

                        authResult = token;
                    }
                    else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
                    {
                    }

                    var reply = await Conversation.ResumeAsync(resumptionCookie, message);
                   
                    var data = await client.Bots.GetPerUserConversationDataAsync(resumptionCookie.BotId, resumptionCookie.ConversationId, resumptionCookie.UserId);
                    reply.SetBotUserData(ContextConstants.AuthResultKey, authResult);
                    int magicNumber = GenerateRandomNumber();
                    reply.SetBotUserData(ContextConstants.MagicNumberKey, magicNumber);
                    reply.SetBotUserData(ContextConstants.MagicNumberValidated, "false");

                    //data.SetProperty(ContextConstants.AuthResultKey, authResult);
                    //data.SetProperty(ContextConstants.MagicNumberKey, magicNumber);
                    //data.SetProperty(ContextConstants.MagicNumberValidated, "false");
                    
                    //await client.Bots.SetUserDataAsync(resumptionCookie.BotId, resumptionCookie.UserId, data);

                   

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
