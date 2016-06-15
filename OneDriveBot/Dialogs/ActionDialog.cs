// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace OneDriveBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using AuthBot;
    using AuthBot.Dialogs;
    using AuthBot.Models;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Newtonsoft.Json.Linq;

    [Serializable]
    public class ActionDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Message> item)
        {
            var message = await item;

            if (string.Equals(message.Text, "help", StringComparison.OrdinalIgnoreCase))
            {
                await context.PostAsync("Hi! I'm a simple OneDrive for Business bot. Just type the keywords you are looking for and I'll search your OneDrive for Business for files.");
                context.Wait(this.MessageReceivedAsync);
            }
            else if (string.Equals(message.Text,"logout",StringComparison.OrdinalIgnoreCase))
            {
                await context.Logout();
                context.Wait(this.MessageReceivedAsync);
            }
            else
            {   //Assume this is a query for OneDrive so let's get an access token
                if (string.IsNullOrEmpty(await context.GetAccessToken(AuthSettings.Scopes)))
                {   
                    //We can't get an access token, so let's try to log the user in
                    await context.Forward(new AzureAuthDialog(AuthSettings.Scopes), this.ResumeAfterAuth, message, CancellationToken.None);                  
                }
                else
                {
                    var files = await SearchOneDrive(message.Text, await context.GetAccessToken(AuthSettings.Scopes));
                    PromptDialog.Choice(context, FileSelectResult, files, "Which file do you want?");
                }
               
            }
        }

        private async Task FileSelectResult(IDialogContext context, IAwaitable<Models.File> file)
        {
            string fileURL = (await file).WebURL;
            await context.PostAsync("Ok, here's link for the file:" + fileURL);
            context.Wait(MessageReceivedAsync);
        }

        private async Task<List<Models.File>> SearchOneDrive(string search,string token)
        {
            List<Models.File> files = new List<Models.File>();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var result = await client.GetAsync("https://graph.microsoft.com/v1.0/me/drive/root/search(q='" + HttpUtility.UrlEncode(search) + "')");
                var resultString = await result.Content.ReadAsStringAsync();

                var jResult = JObject.Parse(resultString);
                JArray jFiles = (JArray)jResult["value"];
                foreach (JObject item in jFiles)
                {
                    Models.File f = new Models.File();
                    f.CreatedBy = item["createdBy"]["user"].Value<string>("displayName");
                    f.CreatedDate = DateTimeOffset.Parse(item.Value<string>("createdDateTime"));
                    f.ID = item.Value<string>("id");
                    f.LastModifiedBy = item["lastModifiedBy"]["user"].Value<string>("displayName");
                    f.LastModifiedDate = DateTimeOffset.Parse(item.Value<string>("lastModifiedDateTime"));
                    f.Name = item.Value<string>("name");
                    f.WebURL = item.Value<string>("webUrl");
                    files.Add(f);
                }
                return files;
            }
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;
            await context.PostAsync(message);
            await context.PostAsync("If you want me to log you off, just say \"logout\". Now what is it that you want me to search for?");
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
