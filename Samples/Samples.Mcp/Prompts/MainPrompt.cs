using Microsoft.Teams.AI.Annotations;

namespace Samples.Mcp.Prompts;

[Prompt("main")]
public class MainPrompt
{
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