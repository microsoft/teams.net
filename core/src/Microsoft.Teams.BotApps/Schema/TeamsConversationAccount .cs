// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Teams.BotApps.Schema;

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
    /// Initializes a new instance of the TeamsConversationAccount class using the specified conversation account.
    /// </summary>
    /// <remarks>If the provided ConversationAccount contains an 'aadObjectId' property as a string, it is
    /// used to set the AadObjectId property of the TeamsConversationAccount.</remarks>
    /// <param name="ca">The ConversationAccount instance containing the conversation's identifier, name, and properties. Cannot be null.</param>
    public TeamsConversationAccount(ConversationAccount ca)
    {
        ArgumentNullException.ThrowIfNull(ca);
        Id = ca.Id ?? string.Empty;
        Name = ca.Name ?? string.Empty;
        if (ca is not null && ca.Properties.TryGetValue("aadObjectId", out object? aadObj)
            && aadObj is JsonElement je
            && je.ValueKind == JsonValueKind.String)
        {
            AadObjectId = je.GetString();
        }
    }
    /// <summary>
    /// Gets or sets the Azure Active Directory (AAD) Object ID associated with the conversation account.
    /// </summary>
    [JsonPropertyName("aadObjectId")] public string? AadObjectId { get; set; }
}
