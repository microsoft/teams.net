using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Extensions;

namespace Samples.Lights;

[Prompt]
[Prompt.Description("manage light status")]
[Prompt.Instructions(
    "The following is a conversation with an AI assistant.",
    "The assistant can turn the lights on or off.",
    "The lights are currently off."
)]
public class LightsPrompt
{
    private IContext<IActivity> Context => _services.GetTeamsContext();
    private readonly IServiceProvider _services;

    public LightsPrompt(IServiceProvider provider)
    {
        _services = provider;
    }

    [Function]
    [Function.Description("get the current light status")]
    public bool GetLightStatus()
    {
        return State.From(Context).Status;
    }

    [Function]
    [Function.Description("turn the lights on")]
    public string LightsOn()
    {
        var state = State.From(Context);
        state.Status = true;
        state.Save(Context);
        return "the lights are now on";
    }

    [Function]
    [Function.Description("turn the lights off")]
    public string LightsOff()
    {
        var state = State.From(Context);
        state.Status = false;
        state.Save(Context);
        return "the lights are now off";
    }
}