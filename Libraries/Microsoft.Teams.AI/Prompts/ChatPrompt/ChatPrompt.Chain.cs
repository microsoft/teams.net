// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI.Prompts;

public partial class ChatPrompt<TOptions>
{
    /// <summary>
    /// provide another chat prompt to be used
    /// as a function
    /// </summary>
    /// <param name="prompt">the chat prompt</param>
    public ChatPrompt<TOptions> Chain(IChatPrompt<TOptions> prompt)
    {
        Functions.Add(new Function(
            prompt.Name,
            prompt.Description,
            JsonSchemaWrapper.CreateObjectSchema(
                ("text", JsonSchemaWrapper.String("text to send"), true)
            ),
            async (string text) =>
            {
                var res = await prompt.Send(text);
                return res.Content;
            }
        ));

        return this;
    }
}