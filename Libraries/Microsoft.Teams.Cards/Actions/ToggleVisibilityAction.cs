using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ToggleVisibilityAction = new("Action.ToggleVisibility");
    public bool IsToggleVisibilityAction => ToggleVisibilityAction.Equals(Value);
}

/// <summary>
/// An action that toggles the visibility of associated card elements.
/// </summary>
public class ToggleVisibilityAction : SelectAction
{
    [JsonPropertyName("targetElements")]
    [JsonPropertyOrder(10)]
    public IList<IUnion<string, TargetElement>> TargetElements { get; set; }

    public ToggleVisibilityAction() : base(CardType.ToggleVisibilityAction)
    {
        TargetElements = [];
    }

    public ToggleVisibilityAction(params string[] targetElements) : base(CardType.ToggleVisibilityAction)
    {
        TargetElements = [];

        foreach (var element in targetElements)
        {
            TargetElements.Add(new Union<string, TargetElement>(element));
        }
    }

    public ToggleVisibilityAction(params TargetElement[] targetElements) : base(CardType.ToggleVisibilityAction)
    {
        TargetElements = [];

        foreach (var element in targetElements)
        {
            TargetElements.Add(new Union<string, TargetElement>(element));
        }
    }

    public ToggleVisibilityAction AddTargets(params string[] value)
    {
        foreach (var element in value)
        {
            TargetElements.Add(new Union<string, TargetElement>(element));
        }

        return this;
    }

    public ToggleVisibilityAction AddTargets(params TargetElement[] value)
    {
        foreach (var element in value)
        {
            TargetElements.Add(new Union<string, TargetElement>(element));
        }

        return this;
    }
}