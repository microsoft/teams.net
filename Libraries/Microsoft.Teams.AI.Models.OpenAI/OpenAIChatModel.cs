// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ClientModel;

using Microsoft.Extensions.Logging;

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
    protected ILogger Logger { get; }

    protected ILoggerFactory LoggerFactory { get; }

    public OpenAIChatModel(string model, OpenAIClient client)
    {
        Model = model;
        ChatClient = client.GetChatClient(model);
        LoggerFactory = Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        Logger = LoggerFactory.CreateLogger($"Microsoft.Teams.AI.{Model}");
    }

    public OpenAIChatModel(string model, string apiKey, Options? options = null)
    {
        options ??= new();
        options.NetworkTimeout ??= TimeSpan.FromSeconds(60);

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        Model = model;
        ChatClient = client.GetChatClient(model);
        LoggerFactory = options.LoggerFactory ?? Extensions.Logging.LoggerFactory.Create( builder => 
        {
            builder.AddConsole();
        });
        Logger = LoggerFactory.CreateLogger($"Microsoft.Teams.AI.{Model}");
    }

    public OpenAIChatModel(string model, ApiKeyCredential apiKey, Options? options = null)
    {
        options ??= new();
        options.NetworkTimeout ??= TimeSpan.FromSeconds(60);

        var client = new OpenAIClient(apiKey, options);
        Model = model;
        ChatClient = client.GetChatClient(model);
        LoggerFactory = options.LoggerFactory ?? Extensions.Logging.LoggerFactory.Create( builder => 
        {
            builder.AddConsole();
        });
        Logger = LoggerFactory.CreateLogger($"Microsoft.Teams.AI.{Model}");
    }
}