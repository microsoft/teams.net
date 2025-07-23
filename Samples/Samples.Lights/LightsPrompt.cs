using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

namespace Samples.Lights;

[Prompt]
[Prompt.Description("manage light status")]
[Prompt.Instructions(
    "The following is a conversation with an AI assistant.",
    "The assistant can turn the lights on or off.",
    "The lights are currently off."
)]
public class LightsPrompt(IContext.Accessor accessor)
{
    private IContext<IActivity> context => accessor.Value!;

    [Function]
    [Function.Description("get the current light status")]
    public bool GetLightStatus()
    {
        return State.From(context).Status;
    }

    [Function]
    [Function.Description("turn the lights on")]
    public string LightsOn()
    {
        var state = State.From(context);
        state.Status = true;
        state.Save(context);
        return "the lights are now on";
    }

    [Function]
    [Function.Description("turn the lights off")]
    public string LightsOff()
    {
        var state = State.From(context);
        state.Status = false;
        state.Save(context);
        return "the lights are now off";
    }
}