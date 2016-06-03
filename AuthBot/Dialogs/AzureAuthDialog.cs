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
                await this.LogIn(context, msg);
            }
        }

        private async Task LogIn(IDialogContext context, Message msg)
        {
            try
            {
                string token = await context.GetAccessToken();

                if (string.IsNullOrEmpty(token))
                {
                    var resumptionCookie = new ResumptionCookie(msg);

                    var authenticationUrl = await AzureActiveDirectoryHelper.GetAuthUrlAsync(resumptionCookie);



                    await context.PostAsync($"You must be authenticated before you can proceed. Please, click [here]({authenticationUrl}) to log into your account.");

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
    }
}
