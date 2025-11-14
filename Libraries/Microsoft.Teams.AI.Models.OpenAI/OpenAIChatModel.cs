// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpenAI;
using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;


public partial class OpenAIChatModel : IChatModel<ChatCompletionOptions>
{
    public string Name => Model;

    /// <summary>
    /// the OpenAI chat client used to
    /// make requests
    /// </summary>
    public ChatClient ChatClient { get; set; }

    /// <summary>
    /// the model name
    /// </summary>
    protected string Model { get; set; }

    /// <summary>
    /// the logger instance
    /// </summary>
    protected ILogger<OpenAIChatModel> Logger { get; }

    public OpenAIChatModel(string model, OpenAIClient client, ILogger<OpenAIChatModel>? logger = null)
    {
        Model = model;
        ChatClient = client.GetChatClient(model);
        Logger = logger ?? NullLogger<OpenAIChatModel>.Instance;
    }

    public OpenAIChatModel(string model, string apiKey, ILogger<OpenAIChatModel>? logger = null, OpenAIClientOptions? options = null)
    {
        options ??= new();
        options.NetworkTimeout ??= TimeSpan.FromSeconds(60);

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        Model = model;
        ChatClient = client.GetChatClient(model);
        Logger = logger ?? NullLogger<OpenAIChatModel>.Instance;
    }

    public OpenAIChatModel(string model, ApiKeyCredential apiKey, ILogger<OpenAIChatModel>? logger = null, OpenAIClientOptions? options = null)
    {
        options ??= new();
        options.NetworkTimeout ??= TimeSpan.FromSeconds(60);

        var client = new OpenAIClient(apiKey, options);
        Model = model;
        ChatClient = client.GetChatClient(model);
        Logger = logger ?? NullLogger<OpenAIChatModel>.Instance;
    }
}
