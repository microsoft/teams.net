// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Templates;

namespace Microsoft.Teams.AI.Prompts;

/// <summary>
/// ChatPrompt Options
/// </summary>
public class ChatPromptOptions
{
    /// <summary>
    /// the name of the prompt
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// the description of the prompt
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// the defining characteristics/objective
    /// of the prompt. This is commonly used to provide a system prompt.
    /// If you supply the system prompt as part of the messages,
    /// you do not need to supply this option.
    /// </summary>
    public ITemplate? Instructions { get; set; }

    /// <summary>
    /// the conversation history
    /// </summary>
    public IList<IMessage>? Messages { get; set; }

    public ChatPromptOptions WithName(string value)
    {
        Name = value;
        return this;
    }

    public ChatPromptOptions WithDescription(string value)
    {
        Description = value;
        return this;
    }

    public ChatPromptOptions WithInstructions(string value)
    {
        Instructions = new StringTemplate(value);
        return this;
    }

    public ChatPromptOptions WithInstructions(params string[] value)
    {
        Instructions = new StringTemplate(string.Join("\n", value));
        return this;
    }

    public ChatPromptOptions WithInstructions(ITemplate value)
    {
        Instructions = value;
        return this;
    }
}