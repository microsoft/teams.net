// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Core.Schema;
using Newtonsoft.Json;

namespace Microsoft.Teams.Bot.Compat;

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
    public static CoreActivity FromCompatActivity(this Activity activity)
    {
        StringBuilder sb = new();
        using StringWriter stringWriter = new(sb);
        using JsonTextWriter json = new(stringWriter);
        BotMessageHandlerBase.BotMessageSerializer.Serialize(json, activity);
        string jsonString = sb.ToString();
        return CoreActivity.FromJsonString(jsonString);
    }





}
