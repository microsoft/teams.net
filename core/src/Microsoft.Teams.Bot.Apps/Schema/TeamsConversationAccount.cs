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
    /// <param name="conversationAccount">The ConversationAccount instance containing the conversation's identifier, name, and properties. Cannot be null.</param>
    public TeamsConversationAccount(ConversationAccount conversationAccount)
    {
        ArgumentNullException.ThrowIfNull(conversationAccount);
        Id = conversationAccount.Id;
        Name = conversationAccount.Name;
        Properties = conversationAccount.Properties;
    }

    /// <summary>
    /// Gets or sets the Azure Active Directory (AAD) Object ID associated with the conversation account.
    /// </summary>
    [JsonIgnore]
    public string? AadObjectId
    {
        get => GetStringProperty("aadObjectId");
        set => Properties["aadObjectId"] = value;
    }

    /// <summary>
    /// Gets or sets given name part of the user name.
    /// </summary>
    [JsonIgnore]
    public string? GivenName
    {
        get => GetStringProperty("givenName");
        set => Properties["givenName"] = value;
    }

    /// <summary>
    /// Gets or sets surname part of the user name.
    /// </summary>
    [JsonIgnore]
    public string? Surname
    {
        get => GetStringProperty("surname");
        set => Properties["surname"] = value;
    }

    /// <summary>
    /// Gets or sets email Id of the user.
    /// </summary>
    [JsonIgnore]
    public string? Email
    {
        get => GetStringProperty("email");
        set => Properties["email"] = value;
    }

    /// <summary>
    /// Gets or sets unique user principal name.
    /// </summary>
    [JsonIgnore]
    public string? UserPrincipalName
    {
        get => GetStringProperty("userPrincipalName");
        set => Properties["userPrincipalName"] = value;
    }

    /// <summary>
    /// Gets or sets the UserRole.
    /// </summary>
    [JsonIgnore]
    public string? UserRole
    {
        get => GetStringProperty("userRole");
        set => Properties["userRole"] = value;
    }

    /// <summary>
    /// Gets or sets the TenantId.
    /// </summary>
    [JsonIgnore]
    public string? TenantId
    {
        get => GetStringProperty("tenantId");
        set => Properties["tenantId"] = value;
    }

    private string? GetStringProperty(string key)
    {
        if (Properties.TryGetValue(key, out var val) && val is JsonElement je && je.ValueKind == JsonValueKind.String)
        {
            return je.GetString();
        }
        return val?.ToString();
    }
}
