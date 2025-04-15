using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType StackLayout = new("Layout.Stack");
    public bool IsStackLayout => StackLayout.Equals(Value);
}

/// <summary>
/// A layout that stacks elements on top of each other. Layout.Stack is the default layout used by AdaptiveCard and all containers.
/// </summary>
public class StackLayout() : Layout(CardType.StackLayout)
{
}