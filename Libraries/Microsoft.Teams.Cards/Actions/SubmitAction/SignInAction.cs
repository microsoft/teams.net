using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class SubmitActionType : StringEnum
{
    public static readonly SubmitActionType SignIn = new("signin");
    public bool IsSignIn => SignIn.Equals(Value);
}

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
            MSTeams = new(value)
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
    [JsonPropertyOrder(0)]
    public new required SignInMSTeamsActionData MSTeams { get; set; }
}

/// <summary>
/// the SignInAction teams data
/// </summary>
public class SignInMSTeamsActionData(string value) : MSTeamsActionData(SubmitActionType.SignIn)
{
    /// <summary>
    /// Set to the `URL` where you want to redirect.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public string Value { get; set; } = value;
}