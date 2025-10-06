using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static Api.SuggestedActions ToTeamsEntity(this Types.SuggestedActions suggestedActions)
    {
        return new()
        {
            To = suggestedActions.To,
            Actions = suggestedActions.Actions.Select(a => a.ToTeamsEntity()).ToList()
        };
    }

    public static Api.Cards.Action ToTeamsEntity(this Types.CardAction action)
    {
        return new(new(action.Type))
        {
            Title = action.Title,
            Text = action.Text,
            DisplayText = action.DisplayText,
            Image = action.Image,
            ImageAltText = action.ImageAltText,
            Value = action.Value,
            ChannelData = action.ChannelData
        };
    }
}

public static partial class AgentExtensions
{
    public static Types.SuggestedActions ToAgentEntity(this Api.SuggestedActions suggestedActions)
    {
        return new()
        {
            To = suggestedActions.To,
            Actions = suggestedActions.Actions.Select(a => a.ToAgentEntity()).ToList()
        };
    }

    public static Types.CardAction ToAgentEntity(this Api.Cards.Action action)
    {
        return new(action.Type)
        {
            Title = action.Title,
            Text = action.Text,
            DisplayText = action.DisplayText,
            Image = action.Image,
            ImageAltText = action.ImageAltText,
            Value = action.Value,
            ChannelData = action.ChannelData
        };
    }
}