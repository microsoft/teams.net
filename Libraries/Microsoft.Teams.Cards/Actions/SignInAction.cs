using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

public class SignInAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(11)]
    public new SignInActionData Data { get; set; }

    public SignInAction(string value)
    {
        Data = new()
        {
            MsTeams = new(value)
        };
    }
}

/// <summary>
/// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
/// </summary>
public class SignInActionData : SubmitActionData
{
    /// <summary>
    /// Teams specific payload data.
    /// </summary>
    [JsonPropertyName("msteams")]
    public new required SignInMSTeamsActionData MsTeams { get; set; }
}

/// <summary>
/// the SignInAction teams data
/// </summary>
public class SignInMSTeamsActionData(string value) : MsTeamsSubmitActionData
{
    /// <summary>
    /// The Teams-specifc sub-type of the action.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; } = "signin";

    /// <summary>
    /// Set to the `URL` where you want to redirect.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = value;
}