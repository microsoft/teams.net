using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType SubmitAction = new("Action.Submit");
    public bool IsSubmitAction => SubmitAction.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<SubmitActionType>))]
public partial class SubmitActionType(string value) : StringEnum(value)
{
}

/// <summary>
/// Gathers input fields, merges with optional data field, and sends an event to the client. It is up to the client to determine how this data is processed. For example: With BotFramework bots, the client would send an activity through the messaging medium to the bot. The inputs that are gathered are those on the current card, and in the case of a show card those on any parent cards. See https://docs.microsoft.com/en-us/adaptive-cards/authoring-cards/input-validation for more details.
/// </summary>
public class SubmitAction() : Action(CardType.SubmitAction)
{
    /// <summary>
    /// Controls which inputs are associated with the action.
    /// </summary>
    [JsonPropertyName("associatedInputs")]
    [JsonPropertyOrder(10)]
    public AssociatedInputs? AssociatedInputs { get; set; }

    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(11)]
    public SubmitActionData? Data { get; set; }

    public SubmitAction WithAssociatedInputs(AssociatedInputs value)
    {
        AssociatedInputs = value;
        return this;
    }

    public SubmitAction WithData(SubmitActionData value)
    {
        Data = value;
        return this;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

/// <summary>
/// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
/// </summary>
public class SubmitActionData
{
    /// <summary>
    /// Teams specific payload data.
    /// </summary>
    [JsonPropertyName("msteams")]
    [JsonPropertyOrder(0)]
    public MSTeamsActionData? MSTeams { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}

/// <summary>
/// Teams specific payload data.
/// </summary>
public class MSTeamsActionData(SubmitActionType type)
{
    /// <summary>
    /// the type of submit action
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public SubmitActionType Type { get; set; } = type;

    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}