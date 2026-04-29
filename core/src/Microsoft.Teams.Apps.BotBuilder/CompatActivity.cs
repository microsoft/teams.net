// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Teams.Core.Schema;
using Newtonsoft.Json;

namespace Microsoft.Teams.Apps.BotBuilder;

/// <summary>
/// Extension methods for converting between Bot Framework Activity and CoreActivity/TeamsActivity.
/// </summary>
public static class CompatActivity
{
    private static string? GetStringValue(object? value) => value switch
    {
        null => null,
        string s => s,
        JsonElement { ValueKind: JsonValueKind.String } el => el.GetString(),
        JsonElement el => el.GetRawText(),
        _ => value.ToString()
    };
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
    public static CoreActivity FromCompatActivity(this Activity activity)
    {
        StringBuilder sb = new();
        using StringWriter stringWriter = new(sb);
        using JsonTextWriter json = new(stringWriter);
        BotMessageHandlerBase.BotMessageSerializer.Serialize(json, activity);
        string jsonString = sb.ToString();
        return CoreActivity.FromJsonString(jsonString);
    }


    /// <summary>
    /// Converts a ConversationAccount to a ChannelAccount.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.ChannelAccount ToCompatChannelAccount(this Microsoft.Teams.Core.Schema.ConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Microsoft.Bot.Schema.ChannelAccount channelAccount;

        channelAccount = new()
        {
            Id = account.Id,
            Name = account.Name
        };


        if (account.Properties.TryGetValue("aadObjectId", out object? aadObjectId))
        {
            channelAccount.AadObjectId = GetStringValue(aadObjectId);
        }

        if (account.Properties.TryGetValue("userRole", out object? userRole))
        {
            channelAccount.Role = GetStringValue(userRole);
        }

        if (account.Properties.TryGetValue("userPrincipalName", out object? userPrincipalName))
        {
            channelAccount.Properties.Add("userPrincipalName", GetStringValue(userPrincipalName) ?? string.Empty);
        }

        if (account.Properties.TryGetValue("givenName", out object? givenName))
        {
            channelAccount.Properties.Add("givenName", GetStringValue(givenName) ?? string.Empty);
        }

        if (account.Properties.TryGetValue("surname", out object? surname))
        {
            channelAccount.Properties.Add("surname", GetStringValue(surname) ?? string.Empty);
        }

        if (account.Properties.TryGetValue("email", out object? email))
        {
            channelAccount.Properties.Add("email", GetStringValue(email) ?? string.Empty);
        }

        if (account.Properties.TryGetValue("tenantId", out object? tenantId))
        {
            channelAccount.Properties.Add("tenantId", GetStringValue(tenantId) ?? string.Empty);
        }

        return channelAccount;
    }

    /// <summary>
    /// Converts a TeamsConversationAccount to a TeamsChannelAccount.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamsChannelAccount ToCompatTeamsChannelAccount2(this Microsoft.Teams.Core.Schema.ConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new Microsoft.Bot.Schema.Teams.TeamsChannelAccount
        {
            Id = account.Id,
            Name = account.Name,
            AadObjectId = GetStringValue(account.Properties["aadObjectId"]) ?? string.Empty,
            Email = GetStringValue(account.Properties["email"]) ?? string.Empty,
            GivenName = GetStringValue(account.Properties["givenName"]) ?? string.Empty,
            Surname = GetStringValue(account.Properties["surname"]) ?? string.Empty,
            UserPrincipalName = GetStringValue(account.Properties["userPrincipalName"]) ?? string.Empty,
            UserRole = GetStringValue(account.Properties["userRole"]) ?? string.Empty,
            TenantId = GetStringValue(account.Properties["tenantId"]) ?? string.Empty
        };
    }


    /// <summary>
    /// Converts a Core PagedMembersResult to a Bot Framework TeamsPagedMembersResult.
    /// </summary>
    /// <param name="pagedMembers"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamsPagedMembersResult ToCompatTeamsPagedMembersResult(this Microsoft.Teams.Core.PagedMembersResult pagedMembers)
    {
        ArgumentNullException.ThrowIfNull(pagedMembers);

        return new Microsoft.Bot.Schema.Teams.TeamsPagedMembersResult
        {
            ContinuationToken = pagedMembers.ContinuationToken,
            Members = pagedMembers.Members?.Select(m => m.ToCompatTeamsChannelAccount()).ToList()
        };
    }

    /// <summary>
    /// Converts a ConversationAccount to a TeamsChannelAccount.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamsChannelAccount ToCompatTeamsChannelAccount(this Microsoft.Teams.Core.Schema.ConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        TeamsChannelAccount teamsChannelAccount = new()
        {
            Id = account.Id,
            Name = account.Name
        };

        // Extract properties from Properties dictionary
        if (account.Properties.TryGetValue("aadObjectId", out object? aadObjectId))
        {
            teamsChannelAccount.AadObjectId = GetStringValue(aadObjectId);
        }

        if (account.Properties.TryGetValue("userPrincipalName", out object? userPrincipalName))
        {
            teamsChannelAccount.UserPrincipalName = GetStringValue(userPrincipalName);
        }

        if (account.Properties.TryGetValue("givenName", out object? givenName))
        {
            teamsChannelAccount.GivenName = GetStringValue(givenName);
        }

        if (account.Properties.TryGetValue("surname", out object? surname))
        {
            teamsChannelAccount.Surname = GetStringValue(surname);
        }

        if (account.Properties.TryGetValue("email", out object? email))
        {
            teamsChannelAccount.Email = GetStringValue(email);
        }

        if (account.Properties.TryGetValue("tenantId", out object? tenantId))
        {
            teamsChannelAccount.Properties.Add("tenantId", GetStringValue(tenantId) ?? string.Empty);
        }

        return teamsChannelAccount;
    }

    /// <summary>
    /// Converts a Bot Framework ChannelAccount to a Core ConversationAccount.
    /// </summary>
    public static Microsoft.Teams.Core.Schema.ConversationAccount FromCompatChannelAccount(this Microsoft.Bot.Schema.ChannelAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Microsoft.Teams.Core.Schema.ConversationAccount result = new() { Id = account.Id, Name = account.Name };

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

    /// <summary>
    /// Converts a Bot Framework ConversationParameters to a Core ConversationParameters.
    /// </summary>
    public static Microsoft.Teams.Core.ConversationParameters FromCompatConversationParameters(this Microsoft.Bot.Schema.ConversationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return new Microsoft.Teams.Core.ConversationParameters
        {
            IsGroup = parameters.IsGroup,
            Bot = parameters.Bot?.FromCompatChannelAccount(),
            Members = parameters.Members?.Select(m => m.FromCompatChannelAccount()).ToList(),
            TopicName = parameters.TopicName,
            Activity = parameters.Activity?.FromCompatActivity(),
            ChannelData = parameters.ChannelData,
            TenantId = parameters.TenantId,
        };
    }

    /// <summary>
    /// Gets the TeamInfo object from the current activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The current activity's team's information, or null.</returns>
    public static TeamInfo? TeamsGetTeamInfo(this IActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        Microsoft.Bot.Schema.Teams.TeamsChannelData channelData = activity.GetChannelData<Microsoft.Bot.Schema.Teams.TeamsChannelData>();
        return channelData?.Team;
    }


}
