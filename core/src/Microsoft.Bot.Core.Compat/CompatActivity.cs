// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Bot.Core.Schema;
using Microsoft.Bot.Schema;
using Microsoft.Teams.BotApps.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Core.Compat;

/// <summary>
/// Extension methods for converting between Bot Framework Activity and CoreActivity/TeamsActivity.
/// </summary>
public static class CompatActivity
{
    /// <summary>
    /// Converts a CoreActivity to a Bot Framework Activity.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static Activity ToCompatActivity(this CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        using JsonTextReader reader = new(new StringReader(activity.ToJson()));
        return BotMessageHandlerBase.BotMessageSerializer.Deserialize<Activity>(reader)!;
    }

    /// <summary>
    /// Converts a Bot Framework Activity to a TeamsActivity.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static TeamsActivity FromCompatActivity(this Activity activity)
    {
        StringBuilder sb = new();
        using StringWriter stringWriter = new(sb);
        using JsonTextWriter json = new(stringWriter);
        BotMessageHandlerBase.BotMessageSerializer.Serialize(json, activity);
        string jsonString = sb.ToString();
        CoreActivity coreActivity = CoreActivity.FromJsonString(jsonString);
        return TeamsActivity.FromActivity(coreActivity);
    }


    /// <summary>
    /// Converts a ConversationAccount to a ChannelAccount.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.ChannelAccount ToCompatChannelAccount(this Microsoft.Bot.Core.Schema.ConversationAccount account)
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
                //Properties = JObject.FromObject(account.Properties)
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
            //channelAccount.UserPrincipalName = userPrincipalName?.ToString();
            channelAccount.Properties.Add("userPrincipalName", userPrincipalName?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("givenName", out object? givenName))
        {
            //channelAccount.GivenName = givenName?.ToString();
            channelAccount.Properties.Add("givenName", givenName?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("surname", out object? surname))
        {
            //channelAccount.Surname = surname?.ToString();
            channelAccount.Properties.Add("surname", surname?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("email", out object? email))
        {
            //channelAccount.Email = email?.ToString();
            channelAccount.Properties.Add("email", email?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("tenantId", out object? tenantId))
        {
            //channelAccount.TenantId = tenantId?.ToString();
            channelAccount.Properties.Add("tenantId", tenantId?.ToString() ?? string.Empty);
        }

        return channelAccount;
    }
}
