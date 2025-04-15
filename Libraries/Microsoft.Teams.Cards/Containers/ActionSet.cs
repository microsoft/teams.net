using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ActionSet = new("ActionSet");
    public bool IsActionSet => ActionSet.Equals(Value);
}

/// <summary>
/// Displays a set of actions.
/// </summary>
public class ActionSet(params Action[] actions) : Element(CardType.ActionSet)
{
    /// <summary>
    /// The array of `Action` elements to show.
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonPropertyOrder(12)]
    public IList<Action> Actions { get; set; } = actions;

    public ActionSet AddActions(params Action[] value)
    {
        foreach (var action in value)
        {
            Actions.Add(action);
        }

        return this;
    }
}