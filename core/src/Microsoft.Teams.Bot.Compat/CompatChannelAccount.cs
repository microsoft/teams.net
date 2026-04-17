// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Compat;

internal static class CompatChannelAccount
{
    internal static Microsoft.Bot.Schema.ChannelAccount ToCompatChannelAccount(this Microsoft.Teams.Bot.Core.Schema.ConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Microsoft.Bot.Schema.ChannelAccount channelAccount;
        if (account is TeamsConversationAccount tae)
        {
            channelAccount = new()
            {
                Id = account.Id,
                Name = account.Name,
                AadObjectId = tae.AadObjectId
            };
        }
        else
        {
            channelAccount = new()
            {
                Id = account.Id,
                Name = account.Name
            };
        }

        if (account.Properties.TryGetValue("aadObjectId", out object? aadObjectId))
        {
            channelAccount.AadObjectId = aadObjectId?.ToString();
        }

        if (account.Properties.TryGetValue("userRole", out object? userRole))
        {
            channelAccount.Role = userRole?.ToString();
        }

        if (account.Properties.TryGetValue("userPrincipalName", out object? userPrincipalName))
        {
            channelAccount.Properties.Add("userPrincipalName", userPrincipalName?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("givenName", out object? givenName))
        {
            channelAccount.Properties.Add("givenName", givenName?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("surname", out object? surname))
        {
            channelAccount.Properties.Add("surname", surname?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("email", out object? email))
        {
            channelAccount.Properties.Add("email", email?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("tenantId", out object? tenantId))
        {
            channelAccount.Properties.Add("tenantId", tenantId?.ToString() ?? string.Empty);
        }

        return channelAccount;
    }

    internal static Microsoft.Teams.Bot.Core.Schema.ConversationAccount FromCompatChannelAccount(this Microsoft.Bot.Schema.ChannelAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Microsoft.Teams.Bot.Core.Schema.ConversationAccount result = new() { Id = account.Id, Name = account.Name };

        if (!string.IsNullOrEmpty(account.AadObjectId))
        {
            result.Properties["aadObjectId"] = account.AadObjectId;
        }

        if (!string.IsNullOrEmpty(account.Role))
        {
            result.Properties["userRole"] = account.Role;
        }

        return result;
    }
}
