// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PABot;

internal static class HttpContextBotExtensions
{
    private static readonly object BotIdKey = new();

    public static string? GetBotId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.Items[BotIdKey] as string;
    }

    public static void SetBotId(this HttpContext httpContext, string botId)
        => httpContext.Items[BotIdKey] = botId;
}
