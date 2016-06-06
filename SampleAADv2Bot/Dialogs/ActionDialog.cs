// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace SampleAADV2Bot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;
    using AuthBot;
    using AuthBot.Dialogs;
    using System.Configuration;
    [Serializable]
    public class ActionDialog : IDialog<string>
    {
      
        private static Lazy<string> mode = new Lazy<string>(() => ConfigurationManager.AppSettings["ActiveDirectory.Mode"]);
        private static Lazy<string> activeDirectoryEndpointUrl = new Lazy<string>(() => ConfigurationManager.AppSettings["ActiveDirectory.EndpointUrl"]);
        private static Lazy<string> activeDirectoryTenant = new Lazy<string>(() => ConfigurationManager.AppSettings["ActiveDirectory.Tenant"]);
        private static Lazy<string> activeDirectoryResourceId = new Lazy<string>(() => ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"]);
        private static Lazy<string> redirectUrl = new Lazy<string>(() => ConfigurationManager.AppSettings["ActiveDirectory.RedirectUrl"]);
        private static Lazy<string> clientId = new Lazy<string>(() => ConfigurationManager.AppSettings["ActiveDirectory.ClientId"]);
        private static Lazy<string> clientSecret = new Lazy<string>(() => ConfigurationManager.AppSettings["ActiveDirectory.ClientSecret"]);
        private static Lazy<string[]> scopes = new Lazy<string[]>(() => ConfigurationManager.AppSettings["ActiveDirectory.Scopes"].Split(','));

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
      
        public async Task TokenSample(IDialogContext context)
        {
            int index = 0;

            //endpoint v1
            var accessToken = await context.GetAccessToken(activeDirectoryResourceId.Value);

            //endpoint v2
            //var accessToken = await context.GetAccessToken(scopes.Value);

            if (string.IsNullOrEmpty(accessToken))
            {
                return;
            }

            await context.PostAsync($"Your access token is: {accessToken}");

            context.Wait(MessageReceivedAsync);
        }


        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Message> item)

        {

            var message = await item;

            context.UserData.SetValue(ContextConstants.CurrentMessageFromKey, message.From);
            context.UserData.SetValue(ContextConstants.CurrentMessageToKey, message.To);

            if (message.Text == "logon")
            {

                //endpoint v2
                //if (string.IsNullOrEmpty(await context.GetAccessToken(scopes.Value)))
                //{
                //    await context.Forward(new AzureAuthDialog(scopes.Value), this.ResumeAfterAuth, message, CancellationToken.None);
                //}
                //else
                //{
                //    context.Wait(MessageReceivedAsync);
                //}

                //endpoint v1
                if (string.IsNullOrEmpty(await context.GetAccessToken(activeDirectoryResourceId.Value)))
                {
                    await context.Forward(new AzureAuthDialog(activeDirectoryResourceId.Value), this.ResumeAfterAuth, message, CancellationToken.None);
                }
                else
                {
                    context.Wait(MessageReceivedAsync);
                }
            }
            else if (message.Text == "echo")
            {

                await context.PostAsync("echo");

                context.Wait(this.MessageReceivedAsync);
            }
            else if (message.Text == "token")
            {
                await TokenSample(context);               
            }
            else if (message.Text == "logout")
            {
                await context.Logout();
                context.Wait(this.MessageReceivedAsync);
            }
            else
            {
                context.Wait(MessageReceivedAsync);
            }

        }
        

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceivedAsync);
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
