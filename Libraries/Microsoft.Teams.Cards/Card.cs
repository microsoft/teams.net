using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// the card type
/// </summary>
[JsonConverter(typeof(JsonConverter<CardType>))]
public partial class CardType(string value) : StringEnum(value)
{
    public static readonly CardType AdaptiveCard = new("AdaptiveCard");
    public bool IsAdaptiveCard => AdaptiveCard.Equals(Value);
}

/// <summary>
/// An Adaptive Card, containing a free-form body of card elements, and an optional set of actions.
/// </summary>
public class Card
{
    /// <summary>
    /// the cards type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public CardType Type { get; } = CardType.AdaptiveCard;

    /// <summary>
    /// The Adaptive Card schema.
    /// </summary>
    [JsonPropertyName("$schema")]
    [JsonPropertyOrder(1)]
    public string? Schema { get; set; }

    /// <summary>
    /// Schema version that this card requires. If a client is lower than this version, the fallbackText will be rendered. NOTE: Version is not required for cards within an Action.ShowCard. However, it is required for the top-level card.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonPropertyOrder(2)]
    public string Version { get; set; } = "1.6";

    /// <summary>
    /// Defines how the card can be refreshed by making a request to the target Bot.
    /// </summary>
    [JsonPropertyName("refresh")]
    [JsonPropertyOrder(3)]
    public Refresh? Refresh { get; set; }

    /// <summary>
    /// Defines authentication information to enable on-behalf-of single sign on or just-in-time OAuth.
    /// </summary>
    [JsonPropertyName("authentication")]
    [JsonPropertyOrder(4)]
    public Auth? Authentication { get; set; }

    /// <summary>
    /// The card elements to show in the primary card region.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(5)]
    public IList<Element>? Body { get; set; }

    /// <summary>
    /// The Actions to show in the card’s action bar.
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonPropertyOrder(6)]
    public IList<Action>? Actions { get; set; }

    /// <summary>
    /// An Action that will be invoked when the card is tapped or selected. Action.ShowCard is not supported.
    /// </summary>
    [JsonPropertyName("selectAction")]
    [JsonPropertyOrder(7)]
    public SelectAction? SelectAction { get; set; }

    /// <summary>
    /// Text shown when the client doesn’t support the version specified (may contain markdown).
    /// </summary>
    [JsonPropertyName("fallbackText")]
    [JsonPropertyOrder(8)]
    public string? FallbackText { get; set; }

    /// <summary>
    /// Specifies the background image of the card.
    /// </summary>
    [JsonPropertyName("backgroundImage")]
    [JsonPropertyOrder(9)]
    public IUnion<BackgroundImage, string>? BackgroundImage { get; set; }

    /// <summary>
    /// Specifies the minimum height of the card.
    /// </summary>
    [JsonPropertyName("minHeight")]
    [JsonPropertyOrder(10)]
    public string? MinHeight { get; set; }

    /// <summary>
    /// When true content in this Adaptive Card should be presented right to left. When ‘false’ content in this Adaptive Card should be presented left to right. If unset, the default platform behavior will apply.
    /// </summary>
    [JsonPropertyName("rtl")]
    [JsonPropertyOrder(11)]
    public bool? Rtl { get; set; }

    /// <summary>
    /// Specifies what should be spoken for this entire card. This is simple text or SSML fragment.
    /// </summary>
    [JsonPropertyName("speak")]
    [JsonPropertyOrder(12)]
    public string? Speak { get; set; }

    /// <summary>
    /// The 2-letter ISO-639-1 language used in the card. Used to localize any date/time functions.
    /// </summary>
    [JsonPropertyName("lang")]
    [JsonPropertyOrder(13)]
    public string? Lang { get; set; }

    /// <summary>
    /// Defines how the content should be aligned vertically within the container. Only relevant for fixed-height cards, or cards with a minHeight specified.
    /// </summary>
    [JsonPropertyName("verticalContentAlignment")]
    [JsonPropertyOrder(14)]
    public VerticalAlignment? VerticalContentAlignment { get; set; }

    [JsonPropertyName("msteams")]
    [JsonPropertyOrder(15)]
    public MSTeamsCardInfo? MSTeams { get; set; }

    public Card WithSchema(string value)
    {
        Schema = value;
        return this;
    }

    public Card WithVersion(string value)
    {
        Version = value;
        return this;
    }

    public Card WithRefresh(Refresh value)
    {
        Refresh = value;
        return this;
    }

    public Card WithAuth(Auth value)
    {
        Authentication = value;
        return this;
    }

    public Card WithSelectAction(SelectAction value)
    {
        SelectAction = value;
        return this;
    }

    public Card WithBody(params Element[] value)
    {
        Body = value;
        return this;
    }

    public Card AddCards(params Element[] value)
    {
        Body ??= [];

        foreach (var element in value)
        {
            Body.Add(element);
        }

        return this;
    }

    public Card AddActions(params Action[] value)
    {
        Actions ??= [];

        foreach (var action in value)
        {
            Actions.Add(action);
        }

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
/// Card metadata for Microsoft Teams.
/// </summary>
public class MSTeamsCardInfo
{
    /// <summary>
    /// Expands the card to take up the full width of the message.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonPropertyOrder(0)]
    public string? Width { get; set; }

    /// <summary>
    /// Conditional visibility of elements on different viewports.
    /// </summary>
    [JsonPropertyName("targetWidth")]
    [JsonPropertyOrder(1)]
    public TargetWidth? TargetWidth { get; set; }
}