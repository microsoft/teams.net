using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Extensions;

namespace Samples.Mcp.Prompts;

[Prompt]
public class MainPrompt
{
    private IContext<IActivity> Context => _services.GetTeamsContext();
    private readonly IServiceProvider _services;

    public MainPrompt(IServiceProvider provider)
    {
        _services = provider;
    }

    [Function("echo")]
    [Function.Description("echos back whatever you said")]
    public string Echo([Param] string text)
    {
        return text;
    }
}