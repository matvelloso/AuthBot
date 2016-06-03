# AuthBot

AuthBot is a .Net library for Azure Active Directory authentication on bots built via Microsoft Bot Framework.

The goals are:

* Support endpoints V1, V2 and B2C (currently only working with v1, we're working on the new MSAL to enable v2 and B2C)

* Allow easy and secure sign in, even in chat sessions including multiple users

* Allow to securely sign out, including clearing browser cookies

* Enable scenarios where bots need to communicate with other services such as Office 365 or Azure by obtaining access tokens


## How does it work?

You can run the SampleAADv1Bot locally using Visual Studio and the Microsoft Bot Framework emulator. The sample allows the user to type the following commands:

* logon: triggers the logon flow, which genrates a hyperlink. The user clicks at the hyperlink, does the normal OpenID connect flow and at the end a magic number is gnerated. The user copies the number back into the chat (this guarantees that the bot won't confuse different users if multiple users attempt to click at the same logon link)
* token: demonstrates how to obtain a token once the user is authenticated
* logout: clears state related to the authentication and generates a hyperlink for the user to to go and also logout from the web browser



