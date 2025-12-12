// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;

BotApplicationBuilder botApplicationBuilder = BotApplication.CreateBuilder();
BotApplication botApplication = botApplicationBuilder.Build();

botApplication.OnActivity = (activity, cancellationToken)
    => botApplication.SendActivityAsync(
        activity.CreateReplyMessageActivity(
            $"You sent: `{activity.Text}` in activity of type `{activity.Type}`."),
            cancellationToken);

botApplication.Run();
