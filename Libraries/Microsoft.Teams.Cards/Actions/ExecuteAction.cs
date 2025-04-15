using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ExecuteAction = new("Action.Execute");
    public bool IsExecuteAction => ExecuteAction.Equals(Value);
}

/// <summary>
/// Gathers input fields, merges with optional data field, and sends an event to the client. Clients process the event by sending an Invoke activity of type adaptiveCard/action to the target Bot. The inputs that are gathered are those on the current card, and in the case of a show card those on any parent cards. See [Universal Action Model](https://docs.microsoft.com/en-us/adaptive-cards/authoring-cards/universal-action-model) documentation for more details.
/// </summary>
public class ExecuteAction() : SelectAction(CardType.ExecuteAction)
{
    /// <summary>
    /// The card author-defined verb associated with this action.
    /// </summary>
    [JsonPropertyName("verb")]
    [JsonPropertyOrder(10)]
    public string? Verb { get; set; }

    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(11)]
    public IUnion<string, IDictionary<string, object>>? Data { get; set; }

    /// <summary>
    /// Controls which inputs are associated with the action.
    /// </summary>
    [JsonPropertyName("associatedInputs")]
    [JsonPropertyOrder(12)]
    public AssociatedInputs? AssociatedInputs { get; set; }

    public ExecuteAction WithVerb(string value)
    {
        Verb = value;
        return this;
    }

    public ExecuteAction WithData(string value)
    {
        Data = new Union<string, IDictionary<string, object>>(value);
        return this;
    }

    public ExecuteAction WithData(IDictionary<string, object> value)
    {
        Data = new Union<string, IDictionary<string, object>>(value);
        return this;
    }

    public ExecuteAction WithAssociatedInputs(AssociatedInputs value)
    {
        AssociatedInputs = value;
        return this;
    }
}