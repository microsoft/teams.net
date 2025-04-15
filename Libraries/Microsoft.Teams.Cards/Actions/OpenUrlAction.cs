using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType OpenUrlAction = new("Action.OpenUrl");
    public bool IsOpenUrlAction => OpenUrlAction.Equals(Value);
}

/// <summary>
/// When invoked, show the given url either by launching it in an external web browser or showing within an embedded web browser.
/// </summary>
public class OpenUrlAction(string url) : SelectAction(CardType.OpenUrlAction)
{
    /// <summary>
    /// The URL to open.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(10)]
    public string Url { get; set; } = url;

    public OpenUrlAction WithUrl(string value)
    {
        Url = value;
        return this;
    }
}