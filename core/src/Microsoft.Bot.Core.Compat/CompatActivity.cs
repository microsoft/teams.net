// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Bot.Core.Schema;
using Microsoft.Bot.Schema;

using Newtonsoft.Json;

namespace Microsoft.Bot.Core.Compat;

internal static class CompatActivity
{
    public static Activity ToCompatActivity(this CoreActivity activity)
    {
        using JsonTextReader reader = new(new StringReader(activity.ToJson()));
        return BotMessageHandlerBase.BotMessageSerializer.Deserialize<Activity>(reader)!;
    }

    public static CoreActivity FromCompatActivity(this Activity activity)
    {
        StringBuilder sb = new();
        using StringWriter stringWriter = new(sb);
        using JsonTextWriter json = new(stringWriter);
        BotMessageHandlerBase.BotMessageSerializer.Serialize(json, activity);
        return CoreActivity.FromJsonString(sb.ToString());
    }

    public static Microsoft.Bot.Schema.ChannelAccount ToCompatChannelAccount(this Microsoft.Bot.Core.Schema.ConversationAccount account)
    {
        ChannelAccount channelAccount = new()
        {
            Id = account.Id,
            Name = account.Name
        };

        // Extract AadObjectId and Role from Properties if they exist
        if (account.Properties.TryGetValue("aadObjectId", out object? aadObjectId))
        {
            channelAccount.AadObjectId = aadObjectId?.ToString();
        }

        if (account.Properties.TryGetValue("role", out object? role))
        {
            channelAccount.Role = role?.ToString();
        }

        return channelAccount;
    }
}
