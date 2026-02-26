// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Core.Tests
{
    internal class AgenticIdentitiyFromEnv
    {

        internal static ConversationAccount GetConversationAccountWithAgenticProperties()
        {
            var agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");
            var agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");
            var agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId");

            if (string.IsNullOrEmpty(agenticUserId))
            {
                return new ConversationAccount();
            }

            ConversationAccount account = new()
            {
                Id = agenticUserId,
                Name = "Agentic User",
                Properties =
            {
                { "agenticUserId", agenticUserId },
                { "agenticAppId", agenticAppId },
                { "agenticAppBlueprintId", agenticAppBlueprintId }
            }
            };
            return account;
        }

        internal static AgenticIdentity GetAgenticIdentity()
        {
            var agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");
            var agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");
            var agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId");

            if (string.IsNullOrEmpty(agenticUserId))
            {
                return null!;
            }

            AgenticIdentity identity = new()
            {
                AgenticUserId = agenticUserId,
                AgenticAppId = agenticAppId,
                AgenticAppBlueprintId = agenticAppBlueprintId
            };
            return identity;
        }
    }
}
