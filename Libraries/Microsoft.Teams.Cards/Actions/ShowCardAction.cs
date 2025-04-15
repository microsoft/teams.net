using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ShowCardAction = new("Action.ShowCard");
    public bool IsShowCardAction => ShowCardAction.Equals(Value);
}

/// <summary>
/// Defines an AdaptiveCard which is shown to the user when the button or link is clicked.
/// </summary>
public class ShowCardAction(Card card) : Action(CardType.ShowCardAction)
{
    /// <summary>
    /// the card to display
    /// </summary>
    [JsonPropertyName("card")]
    [JsonPropertyOrder(10)]
    public Card Card { get; set; } = card;

    public ShowCardAction WithCard(Card value)
    {
        Card = value;
        return this;
    }
}