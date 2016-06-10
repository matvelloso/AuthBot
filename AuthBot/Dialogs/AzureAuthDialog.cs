// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace AuthBot.Dialogs
{
    using System;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Models;

    [Serializable]
    public class AzureAuthDialog : IDialog<string>
    {
        private string resourceId;
        private string[] scopes;

        public AzureAuthDialog(string resourceId)
        {
            this.resourceId = resourceId;
        }
        public AzureAuthDialog(string[] scopes)
        {
            this.scopes = scopes;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Message> argument)
        {
            var msg = await argument;

            AuthResult authResult;
            string validated="";
            int magicNumber = 0;
            if (context.UserData.TryGetValue(ContextConstants.AuthResultKey, out authResult))
            {
                context.UserData.TryGetValue<string>(ContextConstants.MagicNumberValidated, out validated);
                if (validated == "true")
                {
                    context.Done($"Thanks {authResult.UserName}. You are now logged in. ");
                }
                else if (context.UserData.TryGetValue<int>(ContextConstants.MagicNumberKey, out magicNumber))
                { 
                    if (msg.Text==magicNumber.ToString())
                    {
                        context.UserData.SetValue<string>(ContextConstants.MagicNumberValidated, "true");
                        context.Done($"Thanks {authResult.UserName}. You are now logged in. ");
                    }
                    else if (msg.Text==null)
                    {
                        await context.PostAsync($"Please paste back the number you received in your authentication screen.");

                        context.Wait(this.MessageReceivedAsync);
                    }else
                    {
                        context.UserData.RemoveValue(ContextConstants.AuthResultKey);
                        context.UserData.SetValue<string>(ContextConstants.MagicNumberValidated, "false");
                        context.UserData.RemoveValue(ContextConstants.MagicNumberKey);
                        context.Done($"I'm sorry but I couldn't validate your number. Please try authenticating once again. ");
                    }
                }
            }
            else
            {
                if (this.resourceId!=null)
                    await this.LogIn(context, msg,resourceId);
                else
                    await this.LogIn(context, msg, scopes);
                
            }
        }

        private async Task LogIn(IDialogContext context, Message msg, string[] scopes)
        {
            try
            {
                string token = await context.GetAccessToken(scopes);

                if (string.IsNullOrEmpty(token))
                {
                    var resumptionCookie = new ResumptionCookie(msg);

                    var authenticationUrl = await AzureActiveDirectoryHelper.GetAuthUrlAsync(resumptionCookie,scopes);

                    var reply = msg.CreateReplyMessage();
                    reply.To = msg.From;
                    reply.From = msg.To;
                    reply.Text = $"You must be authenticated before you can proceed. Please, click [here]({authenticationUrl}) to log into your account.";
                    await context.PostAsync(reply);

                    context.Wait(this.MessageReceivedAsync);
                }
                else
                {
                    context.Done(string.Empty);
                }
            }catch (Exception ex)
            {
                throw ex;
            }
        }
        private async Task LogIn(IDialogContext context, Message msg, string resourceId)
        {
            try
            {
                string token = await context.GetAccessToken(resourceId);

                if (string.IsNullOrEmpty(token))
                {
                    var resumptionCookie = new ResumptionCookie(msg);

                    var authenticationUrl = await AzureActiveDirectoryHelper.GetAuthUrlAsync(resumptionCookie,resourceId);

                    await context.PostAsync($"You must be authenticated before you can proceed. Please, click [here]({authenticationUrl}) to log into your account.");

                    context.Wait(this.MessageReceivedAsync);
                }
                else
                {
                    context.Done(string.Empty);
                }
            }
            catch (Exception ex)
            {
                throw ex;
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
