// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models;

namespace Microsoft.Teams.AI.Prompts;

public partial class ChatPrompt<TOptions>
{
    public async Task<IMessage> Send(IMessage message, CancellationToken cancellationToken = default)
    {
        return await Send(message, null, null, cancellationToken);
    }

    public Task<ModelMessage<string>> Send(string text, IChatPrompt<TOptions>.RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default)
    {
        var message = UserMessage.Text(text);
        return Send((IMessage)message, options, onChunk, cancellationToken);
    }

    public Task<ModelMessage<string>> Send(IContent[] content, IChatPrompt<TOptions>.RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default)
    {
        var message = UserMessage.Text(content);
        return Send((IMessage)message, options, onChunk, cancellationToken);
    }

    public Task<ModelMessage<string>> Send(UserMessage<string> message, IChatPrompt<TOptions>.RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default)
    {
        return Send((IMessage)message, options, onChunk, cancellationToken);
    }

    public Task<ModelMessage<string>> Send(UserMessage<IEnumerable<IContent>> message, IChatPrompt<TOptions>.RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default)
    {
        return Send((IMessage)message, options, onChunk, cancellationToken);
    }

    public async Task<ModelMessage<string>> Send(IMessage message, IChatPrompt<TOptions>.RequestOptions? options = null, OnStreamChunk? onChunk = null, CancellationToken cancellationToken = default)
    {
        var messages = options?.Messages ?? Messages;
        var buffer = string.Empty;
        var prompt = Template is not null ? await Template.Render(null, cancellationToken) : null;

        async Task OnChunk(string chunk)
        {
            if (chunk == string.Empty || onChunk is null) return;
            buffer += chunk;

            try
            {
                await onChunk(buffer);
                buffer = string.Empty;
            }
            catch { return; }
        }

        var functions = new FunctionCollection();
        foreach (var kvp in Functions)
        {
            functions[kvp.Key] = kvp.Value;
        }
        var instructions = prompt is null ? null : new DeveloperMessage(prompt);

        // allow plugins to modify functions and instructions before each send
        foreach (var plugin in Plugins)
        {
            functions = await plugin.OnBuildFunctions(this, functions, cancellationToken);
            instructions = await plugin.OnBuildInstructions(this, instructions);
        }

        ChatModelOptions<TOptions> requestOptions = new(Invoke(functions))
        {
            Functions = functions.List,
            Messages = messages,
            Prompt = instructions,
            Options = options is null ? default : options.Request,
        };

        ModelMessage<string>? res;

        try
        {
            Logger.Debug(message);

            foreach (var plugin in Plugins)
            {
                message = await plugin.OnBeforeSend(this, message, requestOptions.Options, cancellationToken);
            }

            if (onChunk is null)
            {
                res = await Model.Send(message, requestOptions, cancellationToken);
            }
            else
            {
                res = await Model.Send(message, requestOptions, new Stream(OnChunk), cancellationToken);
            }

            Logger.Debug(res);

            foreach (var plugin in Plugins)
            {
                res = (ModelMessage<string>)await plugin.OnAfterSend(this, res, requestOptions.Options, cancellationToken);
            }

            return res;
        }
        catch (Exception ex)
        {
            ErrorEvent(Model, ex);
            throw new Exception("an error occurred while attempting to send the message", ex);
        }
    }
}