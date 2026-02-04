// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

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
    /// <remarks>If the provided ConversationAccount contains Teams-specific properties in the Properties dictionary
    /// (such as 'aadObjectId', 'givenName', 'surname', 'email', 'userPrincipalName', 'userRole', 'tenantId'),
    /// they are extracted and used to populate the corresponding properties of the TeamsConversationAccount.</remarks>
    /// <param name="conversationAccount">The ConversationAccount instance containing the conversation's identifier, name, and properties. Cannot be null.</param>
    public TeamsConversationAccount(ConversationAccount conversationAccount)
    {
        ArgumentNullException.ThrowIfNull(conversationAccount);
        Id = conversationAccount.Id;
        Name = conversationAccount.Name;

        // Extract properties from the Properties dictionary
        if (conversationAccount.Properties.TryGetValue("aadObjectId", out object? aadObj)
            && aadObj is JsonElement aadJe
            && aadJe.ValueKind == JsonValueKind.String)
        {
            AadObjectId = aadJe.GetString();
        }

        if (conversationAccount.Properties.TryGetValue("givenName", out object? givenNameObj)
            && givenNameObj is JsonElement givenNameJe
            && givenNameJe.ValueKind == JsonValueKind.String)
        {
            GivenName = givenNameJe.GetString();
        }

        if (conversationAccount.Properties.TryGetValue("surname", out object? surnameObj)
            && surnameObj is JsonElement surnameJe
            && surnameJe.ValueKind == JsonValueKind.String)
        {
            Surname = surnameJe.GetString();
        }

        if (conversationAccount.Properties.TryGetValue("email", out object? emailObj)
            && emailObj is JsonElement emailJe
            && emailJe.ValueKind == JsonValueKind.String)
        {
            Email = emailJe.GetString();
        }

        if (conversationAccount.Properties.TryGetValue("userPrincipalName", out object? upnObj)
            && upnObj is JsonElement upnJe
            && upnJe.ValueKind == JsonValueKind.String)
        {
            UserPrincipalName = upnJe.GetString();
        }

        if (conversationAccount.Properties.TryGetValue("userRole", out object? roleObj)
            && roleObj is JsonElement roleJe
            && roleJe.ValueKind == JsonValueKind.String)
        {
            UserRole = roleJe.GetString();
        }

        if (conversationAccount.Properties.TryGetValue("tenantId", out object? tenantObj)
            && tenantObj is JsonElement tenantJe
            && tenantJe.ValueKind == JsonValueKind.String)
        {
            TenantId = tenantJe.GetString();
        }
    }
    /// <summary>
    /// Gets or sets the Azure Active Directory (AAD) Object ID associated with the conversation account.
    /// </summary>
    [JsonPropertyName("aadObjectId")] public string? AadObjectId { get; set; }

    /// <summary>
    /// Gets or sets given name part of the user name.
    /// </summary>
    [JsonPropertyName("givenName")] public string? GivenName { get; set; }

    /// <summary>
    /// Gets or sets surname part of the user name.
    /// </summary>
    [JsonPropertyName("surname")] public string? Surname { get; set; }

    /// <summary>
    /// Gets or sets email Id of the user.
    /// </summary>
    [JsonPropertyName("email")] public string? Email { get; set; }

    /// <summary>
    /// Gets or sets unique user principal name.
    /// </summary>
    [JsonPropertyName("userPrincipalName")] public string? UserPrincipalName { get; set; }

    /// <summary>
    /// Gets or sets the UserRole.
    /// </summary>
    [JsonPropertyName("userRole")] public string? UserRole { get; set; }

    /// <summary>
    /// Gets or sets the TenantId.
    /// </summary>
    [JsonPropertyName("tenantId")] public string? TenantId { get; set; }
}
