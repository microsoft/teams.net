using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Microsoft.Teams.Extensions.Graph;

var builder = WebApplication.CreateBuilder(args);

var appBuilder = App.Builder()
    .AddLogger(level: Microsoft.Teams.Common.Logging.LogLevel.Debug)
    // The name of the auth connection to use.
    // It should be the same as the OAuth connection name defined in the Azure Bot configuration.
    .AddOAuth("graph");

builder.AddTeams(appBuilder);

var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage("/signout", async context =>
{
    if (!context.IsSignedIn)
    {
        await context.Send("you are not signed in!");
        return;
    }

    await context.SignOut(); // call `SignOut()` for your auth connection...
    await context.Send("you have been signed out!");
});

teams.OnMessage(async context =>
{
    if (!context.IsSignedIn)
    {
        await context.SignIn(new SignInOptions()
        {
            // Customize the OAuth card text (only applies to OAuth flow, not SSO)
            OAuthCardText = "Sign in to your account",
            SignInButtonText = "Sign In"
        }); // call `SignIn() for your auth connection...

        return;
    }

    // If user is not signed in then `GetUserGraphClient` will throw an exception
    var me = await context.GetUserGraphClient().Me.GetAsync();
    await context.Send($"user '{me!.DisplayName}' is already signed in!");
});

teams.OnSignIn(async (_, @event) =>
{
    var token = @event.Token;
    var context = @event.Context;

    var me = await context.GetUserGraphClient().Me.GetAsync();
    await context.Send($"user \"{me!.DisplayName}\" signed in. Here's the token: {token.Token}");
});

app.Run();