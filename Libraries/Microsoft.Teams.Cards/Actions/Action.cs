using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Controls the style of an Action, which influences how the action is displayed, spoken, etc.
/// </summary>
[JsonConverter(typeof(JsonConverter<ActionStyle>))]
public partial class ActionStyle(string value) : StringEnum(value)
{
    public static readonly ActionStyle Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly ActionStyle Positive = new("positive");
    public bool IsPositive => Positive.Equals(Value);

    public static readonly ActionStyle Destructive = new("destructive");
    public bool IsDestructive => Destructive.Equals(Value);
}

/// <summary>
/// Determines whether an action is displayed with a button or is moved to the overflow menu.
/// </summary>
[JsonConverter(typeof(JsonConverter<ActionMode>))]
public partial class ActionMode(string value) : StringEnum(value)
{
    public static readonly ActionMode Primary = new("primary");
    public bool IsPrimary => Primary.Equals(Value);

    public static readonly ActionMode Secondary = new("secondary");
    public bool IsSecondary => Secondary.Equals(Value);
}

public abstract class Action(CardType type)
{
    /// <summary>
    /// A unique identifier associated with this Action.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string? Id { get; set; }

    /// <summary>
    /// the action type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public CardType Type { get; set; } = type;

    /// <summary>
    /// Label for button or link that represents this action.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(2)]
    public string? Title { get; set; }

    /// <summary>
    /// Optional icon to be shown on the action in conjunction with the title. Supports data URI in version 1.2+
    /// </summary>
    [JsonPropertyName("iconUrl")]
    [JsonPropertyOrder(3)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Controls the style of an Action, which influences how the action is displayed, spoken, etc.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(4)]
    public ActionStyle? Style { get; set; }

    /// <summary>
    /// Describes what to do when an unknown element is encountered or the requires of this or any children can’t be met.
    /// </summary>
    [JsonPropertyName("fallback")]
    [JsonPropertyOrder(5)]
    public IUnion<Action, string>? Fallback { get; set; }

    /// <summary>
    /// Determines whether an action is displayed with a button or is moved to the overflow menu.
    /// </summary>
    [JsonPropertyName("mode")]
    [JsonPropertyOrder(6)]
    public ActionMode? Mode { get; set; }

    /// <summary>
    /// Defines text that should be displayed to the end user as they hover the mouse over the action, and read when using narration software.
    /// </summary>
    [JsonPropertyName("tooltip")]
    [JsonPropertyOrder(7)]
    public string? Tooltip { get; set; }

    /// <summary>
    /// Determines whether the action should be enabled.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    [JsonPropertyOrder(8)]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// A series of key/value pairs indicating features that the item requires with corresponding minimum version. When a feature is missing or of insufficient version, fallback is triggered.
    /// </summary>
    [JsonPropertyName("requires")]
    [JsonPropertyOrder(9)]
    public IDictionary<string, string>? Requires { get; set; }

    public Action WithId(string value)
    {
        Id = value;
        return this;
    }

    public Action WithTitle(string value)
    {
        Title = value;
        return this;
    }

    public Action WithIconUrl(string value)
    {
        IconUrl = value;
        return this;
    }

    public Action WithStyle(ActionStyle value)
    {
        Style = value;
        return this;
    }

    public Action WithFallback(Action value)
    {
        Fallback = new Union<Action, string>(value);
        return this;
    }

    public Action WithFallback(string value)
    {
        Fallback = new Union<Action, string>(value);
        return this;
    }

    public Action WithMode(ActionMode value)
    {
        Mode = value;
        return this;
    }

    public Action WithTooltip(string value)
    {
        Tooltip = value;
        return this;
    }

    public Action WithEnabled(bool value = true)
    {
        IsEnabled = value;
        return this;
    }

    public Action WithRequires(IDictionary<string, string> value)
    {
        Requires = value;
        return this;
    }

    public Action WithRequire(string key, string value)
    {
        Requires ??= new Dictionary<string, string>();
        Requires.Add(key, value);
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