using System.Diagnostics;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services
    .AddTeamsBotApplication()
    .AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis"); // requires a Redis instance, for local testing you can use Docker: `docker run -p 6379:6379 redis`
    });

var app = builder.Build();

var bot = app.UseTeamsBotApplication();

bot.OnMessage(async (ctx, ct) =>
{
    var clock = Stopwatch.StartNew();
    var text = ctx.Activity.Text;
    var replyText = $"Echo: {text}";
    await ctx.SendActivityAsync(replyText, ct);

    var diagnosticInfo = $"sdk version: {TeamsBotApplication.Version} os: {Environment.OSVersion}";
    diagnosticInfo += $" auth latency: {clock.Elapsed.TotalMilliseconds}ms";
    await ctx.SendActivityAsync(diagnosticInfo, ct);
});

app.Run();
