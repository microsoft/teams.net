// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI.Prompts;

public partial class ChatPrompt<TOptions>
{
    public ChatPrompt<TOptions> Plugin(IPlugin plugin)
    {
        Plugins.Add(plugin);
        return this;
    }

    public ChatPrompt<TOptions> Plugin(IChatPlugin plugin)
    {
        Plugins.Add(plugin);
        return this;
    }
}