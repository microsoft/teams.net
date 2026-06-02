// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents a Microsoft Teams-specific conversation account, including Azure Active Directory (AAD) object
/// information.
/// </summary>
/// <remarks>This class extends the base ConversationAccount to provide additional properties relevant to
/// Microsoft Teams, such as the Azure Active Directory object ID. It is typically used when working with Teams
/// conversations to access Teams-specific metadata.</remarks>
public class TeamsConversationAccount : ConversationAccount
{
    /// <summary>
    /// Initializes a new instance of the TeamsConversationAccount class.
    /// </summary>
    [JsonConstructor]
    public TeamsConversationAccount()
    {
    }

    /// <summary>
    /// Initializes a new instance of the TeamsConversationAccount class using the specified conversation account.
    /// </summary>
    /// <param name="conversationAccount">The ConversationAccount instance containing the conversation's identifier, name, and properties. Cannot be null.</param>
    public static TeamsConversationAccount? FromConversationAccount(ConversationAccount? conversationAccount)
    {
        if (conversationAccount is null)
        {
            return null;
        }

        if (conversationAccount is TeamsConversationAccount teamsConversationAccount)
        {
            return teamsConversationAccount;
        }

        TeamsConversationAccount result = new();
        result.Id = conversationAccount.Id;
        result.Name = conversationAccount.Name;
        result.IsTargeted = conversationAccount.IsTargeted;
        result.AgenticAppId = conversationAccount.AgenticAppId;
        result.AgenticUserId = conversationAccount.AgenticUserId;
        result.AgenticAppBlueprintId = conversationAccount.AgenticAppBlueprintId;
        result.Properties = new ExtendedPropertiesDictionary(conversationAccount.Properties);
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
    /// Gets or sets the Azure Active Directory (AAD) Object ID associated with the conversation account.
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

    /// <summary>
    /// Gets or sets the Microsoft Entra tenant ID associated with this account.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }
}
