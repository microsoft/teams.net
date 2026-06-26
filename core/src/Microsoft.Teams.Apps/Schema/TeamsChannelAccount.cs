// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents a Microsoft Teams-specific channel account (a participant identity), including Azure Active
/// Directory (AAD) object information.
/// </summary>
/// <remarks>This class extends the base <see cref="ChannelAccount"/> to provide additional properties relevant to
/// Microsoft Teams, such as the Azure Active Directory object ID. It is typically used when working with Teams
/// participants to access Teams-specific metadata.</remarks>
public class TeamsChannelAccount : ChannelAccount
{
    /// <summary>
    /// Initializes a new instance of the TeamsChannelAccount class.
    /// </summary>
    [JsonConstructor]
    public TeamsChannelAccount()
    {
    }

    /// <summary>
    /// Initializes a new instance of the TeamsChannelAccount class from an existing channel account.
    /// </summary>
    /// <param name="channelAccount">The ChannelAccount instance containing the participant's identifier, name, and properties. Cannot be null.</param>
    public static TeamsChannelAccount? FromChannelAccount(ChannelAccount? channelAccount)
    {
        if (channelAccount is null)
        {
            return null;
        }

        if (channelAccount is TeamsChannelAccount teamsChannelAccount)
        {
            return teamsChannelAccount;
        }

        TeamsChannelAccount result = new();
        result.Id = channelAccount.Id;
        result.Name = channelAccount.Name;
        result.IsTargeted = channelAccount.IsTargeted;
        result.AgenticAppId = channelAccount.AgenticAppId;
        result.AgenticUserId = channelAccount.AgenticUserId;
        result.AgenticAppBlueprintId = channelAccount.AgenticAppBlueprintId;
        result.Properties = new ExtendedPropertiesDictionary(channelAccount.Properties);
        result.AadObjectId = result.Properties.Extract<string>("aadObjectId");
        result.ObjectId = result.Properties.Extract<string>("objectId");
        result.GivenName = result.Properties.Extract<string>("givenName");
        result.Surname = result.Properties.Extract<string>("surname");
        result.Email = result.Properties.Extract<string>("email");
        result.UserPrincipalName = result.Properties.Extract<string>("userPrincipalName");
        result.UserRole = result.Properties.Extract<string>("userRole");
        result.TenantId = result.Properties.Extract<string>("tenantId");
        if (string.IsNullOrEmpty(result.AadObjectId))
        {
            result.AadObjectId = result.ObjectId;
        }
        return result;
    }

    /// <summary>
    /// Gets or sets the Azure Active Directory (AAD) Object ID associated with the channel account.
    /// </summary>
    [JsonPropertyName("aadObjectId")]
    public string? AadObjectId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user in the conversation.
    /// </summary>
    [JsonPropertyName("objectId")]
    public string? ObjectId { get; set; }

    /// <summary>
    /// Gets or sets given name part of the user name.
    /// </summary>
    [JsonPropertyName("givenName")]
    public string? GivenName { get; set; }

    /// <summary>
    /// Gets or sets surname part of the user name.
    /// </summary>
    [JsonPropertyName("surname")]
    public string? Surname { get; set; }

    /// <summary>
    /// Gets or sets email Id of the user.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets unique user principal name.
    /// </summary>
    [JsonPropertyName("userPrincipalName")]
    public string? UserPrincipalName { get; set; }

    /// <summary>
    /// Gets or sets the UserRole.
    /// </summary>
    [JsonPropertyName("userRole")]
    public string? UserRole { get; set; }
}
