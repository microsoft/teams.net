// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Json.Schema;

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
            new JsonSchemaBuilder().Properties(
                ("text", new JsonSchemaBuilder().Type(SchemaValueType.String).Description("text to send"))
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