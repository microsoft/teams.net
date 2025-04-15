using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Defines authentication information associated with a card. This maps to the OAuthCard type defined by the Bot Framework (https://docs.microsoft.com/dotnet/api/microsoft.bot.schema.oauthcard)
/// </summary>
public class Auth
{
    /// <summary>
    /// Text that can be displayed to the end user when prompting them to authenticate.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(0)]
    public string? Text { get; set; }

    /// <summary>
    /// The identifier for registered OAuth connection setting information.
    /// </summary>
    [JsonPropertyName("connectionName")]
    [JsonPropertyOrder(1)]
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Provides information required to enable on-behalf-of single sign-on user authentication.
    /// </summary>
    [JsonPropertyName("tokenExchangeResource")]
    [JsonPropertyOrder(2)]
    public TokenExchangeResource? TokenExchangeResource { get; set; }

    /// <summary>
    /// Buttons that should be displayed to the user when prompting for authentication. The array MUST contain one button of type “signin”. Other button types are not currently supported.
    /// </summary>
    [JsonPropertyName("buttons")]
    [JsonPropertyOrder(3)]
    public IList<AuthCardButton>? Buttons { get; set; }

    public Auth WithText(string value)
    {
        Text = value;
        return this;
    }

    public Auth WithConnectionName(string value)
    {
        ConnectionName = value;
        return this;
    }

    public Auth WithTokenExchangeResource(TokenExchangeResource value)
    {
        TokenExchangeResource = value;
        return this;
    }

    public Auth AddButtons(params AuthCardButton[] value)
    {
        Buttons ??= [];

        foreach (var button in value)
        {
            Buttons.Add(button);
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