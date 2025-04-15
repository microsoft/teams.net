using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Defines a button as displayed when prompting a user to authenticate. This maps to the cardAction type defined by the Bot Framework (https://docs.microsoft.com/dotnet/api/microsoft.bot.schema.cardaction).
/// </summary>
public class AuthCardButton
{
    /// <summary>
    /// The type of the button.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public required string Type { get; set; }

    /// <summary>
    /// The value associated with the button. The meaning of value depends on the button’s type.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public required string Value { get; set; }

    /// <summary>
    /// The caption of the button.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(2)]
    public string? Title { get; set; }

    /// <summary>
    /// A URL to an image to display alongside the button’s caption.
    /// </summary>
    [JsonPropertyName("image")]
    [JsonPropertyOrder(3)]
    public string? Image { get; set; }

    public AuthCardButton WithType(string value)
    {
        Type = value;
        return this;
    }

    public AuthCardButton WithValue(string value)
    {
        Value = value;
        return this;
    }

    public AuthCardButton WithTitle(string value)
    {
        Title = value;
        return this;
    }

    public AuthCardButton WithImage(string value)
    {
        Image = value;
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