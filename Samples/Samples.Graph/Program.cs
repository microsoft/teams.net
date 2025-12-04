using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Extensions.Graph;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();
builder.Services.Configure<AppOptions>(options => options.OAuth = new OAuthSettings("graph"));
var app = builder.Build();
var teams = app.UseTeams();

teams.Use(async context =>
{
    var start = DateTime.UtcNow;
    try
    {
        await context.Next();
    }
    catch
    {
        context.Log.Error("error occurred during activity processing");
    }
    context.Log.Debug($"request took {(DateTime.UtcNow - start).TotalMilliseconds}ms");
});

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
        await context.SignIn(new OAuthOptions()
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