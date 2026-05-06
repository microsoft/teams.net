using Microsoft.Teams.Apps;
using Microsoft.Teams.Plugins.AspNetCore;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Samples.Repro.DA;

// ---------------------------------------------------------------------------
// This sample reproduces the Declarative Agents setup pattern where:
//
//   1. An App is created manually (not via the default UseTeams() Minimal API path)
//   2. AspNetCorePlugin is added to the App
//   3. A [TeamsController] message controller is added via app.AddController()
//   4. A custom ASP.NET Core API controller receives HTTP POSTs and calls
//      app.GetPlugin<AspNetCorePlugin>().Do(HttpContext, cancellationToken)
//
// This mirrors the production DeclarativeAgentsChatBot flow:
//   DeclarativeAgentsChatBotController (API controller, custom route)
//     -> resolves keyed App from DI
//     -> app.GetPlugin<AspNetCorePlugin>().Do(HttpContext, ct)
//     -> routes to DeclarativeAgentsChatBotMessageController ([TeamsController])
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// Register Teams token authentication (JWT validation) and controllers.
// Pass routing: false so the Teams plugin does NOT register its own /api/messages endpoint —
// BotController provides the /api/messages route and forwards into the SDK pipeline.
builder.AddTeams(routing: false);
builder.Services.AddControllers();

// Build the App manually, matching the production CreateApp() pattern.
var messageController = new MessageController();

var teamsApp = new App();
teamsApp.AddPlugin(new AspNetCorePlugin());
#pragma warning disable CS0618 // AddController is obsolete in favor of Minimal APIs
teamsApp.AddController(messageController);
#pragma warning restore CS0618

// Register the App instance in DI so BotController can inject it.
builder.Services.AddSingleton(teamsApp);

var app = builder.Build();

// Start the Teams App (initializes plugins, acquires bot token).
await teamsApp.Start();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
