using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// an element that contains other elements
/// </summary>
public abstract class ContainerElement(CardType type) : Element(type)
{
    /// <summary>
    /// The layouts associated with the container. The container can dynamically switch from one layout to another as the card's width changes. See Container layouts for more details.
    /// </summary>
    [JsonPropertyName("layouts")]
    [JsonPropertyOrder(12)]
    public IList<Layout>? Layouts { get; set; }

    /// <summary>
    /// Specifies the background image. Acceptable formats are PNG, JPEG, and GIF
    /// </summary>
    [JsonPropertyName("backgroundImage")]
    [JsonPropertyOrder(13)]
    public IUnion<string, BackgroundImage>? BackgroundImage { get; set; }

    /// <summary>
    /// Determines whether the column should bleed through its parent's padding.
    /// </summary>
    [JsonPropertyName("bleed")]
    [JsonPropertyOrder(14)]
    public bool? Bleed { get; set; }

    /// <summary>
    /// Controls if the container should have rounded corners.
    /// </summary>
    [JsonPropertyName("roundedCorners")]
    [JsonPropertyOrder(15)]
    public bool? RoundedCorners { get; set; }

    /// <summary>
    /// When `true` content in this container should be presented right to left. When 'false' content in this container should be presented left to right. When unset layout direction will inherit from parent container or column. If unset in all ancestors, the default platform behavior will apply.
    /// </summary>
    [JsonPropertyName("rtl")]
    [JsonPropertyOrder(16)]
    public bool? Rtl { get; set; }

    /// <summary>
    /// Controls if a border should be displayed around the container.
    /// </summary>
    [JsonPropertyName("showBorder")]
    [JsonPropertyOrder(17)]
    public bool? ShowBorder { get; set; }

    /// <summary>
    /// An Action that will be invoked when the `Container` is tapped or selected. `Action.ShowCard` is not supported.
    /// </summary>
    [JsonPropertyName("selectAction")]
    [JsonPropertyOrder(18)]
    public SelectAction? SelectAction { get; set; }

    public ContainerElement WithLayouts(params Layout[] value)
    {
        Layouts = value;
        return this;
    }

    public ContainerElement WithBackgroundImage(string value)
    {
        BackgroundImage = new Union<string, BackgroundImage>(value);
        return this;
    }

    public ContainerElement WithBackgroundImage(BackgroundImage value)
    {
        BackgroundImage = new Union<string, BackgroundImage>(value);
        return this;
    }

    public ContainerElement WithBleed(bool value = true)
    {
        Bleed = value;
        return this;
    }

    public ContainerElement WithRoundedCorners(bool value = true)
    {
        RoundedCorners = value;
        return this;
    }

    public ContainerElement WithRtl(bool value = true)
    {
        Rtl = value;
        return this;
    }

    public ContainerElement WithShowBorder(bool value = true)
    {
        ShowBorder = value;
        return this;
    }

    public ContainerElement WithSelectAction(SelectAction value)
    {
        SelectAction = value;
        return this;
    }
}