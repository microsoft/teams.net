using System.Text;
using System.Text.Json;

using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models.OpenAI.Builders;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public partial class OpenAIChatModel
{
    public async Task<IMessage> Send(IMessage message, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var res = await Send(message, new ChatModelOptions<ChatCompletionOptions>()
        {
            Functions = [],
            Messages = []
        }, cancellationToken);

        return res;
    }

    public async Task<ModelMessage<string>> Send(IMessage message, ChatModelOptions<ChatCompletionOptions> options, CancellationToken cancellationToken = default)
    {
        var messages = await CallFunctions(message, options, cancellationToken);
        var chatMessages = messages.Select(m => m.ToOpenAI()).ToList();

        if (options.Prompt != null)
        {
            chatMessages.Insert(0, options.Prompt.ToOpenAI());
        }

        var tools = options.Functions.Select(function => function.ToOpenAI()).ToArray();

        try
        {
            var requestOptions = options.Options ?? new ChatCompletionOptions();

            foreach (var tool in tools)
            {
                requestOptions.Tools.Add(tool);
            }

            var result = await ChatClient.CompleteChatAsync(
                chatMessages,
                requestOptions,
                cancellationToken
            );

            var modelMessage = ChatMessage.CreateAssistantMessage(result.Value).ToTeams();

            if (modelMessage.HasFunctionCalls)
            {
                return await Send(modelMessage, options, cancellationToken);
            }

            messages.Add(modelMessage);
            return modelMessage;
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
            throw new Exception("chat completion error", ex);
        }
    }

    public async Task<ModelMessage<string>> Send(IMessage message, ChatModelOptions<ChatCompletionOptions> options, IStream stream, CancellationToken cancellationToken = default)
    {
        var messages = await CallFunctions(message, options, cancellationToken);
        var chatMessages = messages.Select(m => m.ToOpenAI()).ToList();

        if (options.Prompt != null)
        {
            chatMessages.Insert(0, options.Prompt.ToOpenAI());
        }

        var tools = options.Functions.Select(function => function.ToOpenAI()).ToArray();

        try
        {
            var requestOptions = options.Options ?? new ChatCompletionOptions();

            foreach (var tool in tools)
            {
                requestOptions.Tools.Add(tool);
            }

            var res = ChatClient.CompleteChatStreamingAsync(chatMessages, requestOptions, cancellationToken);
            var content = new StringBuilder();
            var toolCalls = new StreamingChatToolCallsBuilder();

            await foreach (var chunk in res)
            {
                var delta = new StringBuilder();

                foreach (var update in chunk.ContentUpdate)
                {
                    delta.Append(update.Text);
                }

                foreach (var update in chunk.ToolCallUpdates)
                {
                    toolCalls.Append(update);
                }

                content.Append(delta);
                stream.Emit(delta.ToString());

                if (chunk.FinishReason == ChatFinishReason.ToolCalls)
                {
                    var input = ChatMessage.CreateAssistantMessage(toolCalls.Build()).ToTeams();
                    return await Send(input, options, stream, cancellationToken);
                }
                else if (chunk.FinishReason == ChatFinishReason.Length)
                {
                    throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");
                }
                else if (chunk.FinishReason == ChatFinishReason.ContentFilter)
                {
                    throw new NotImplementedException("Omitted content due to a content filter flag.");
                }
            }

            var modelMessage = ChatMessage.CreateAssistantMessage(content.ToString()).ToTeams();
            messages.Add(modelMessage);
            return modelMessage;
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
            throw new Exception("chat completion error", ex);
        }
    }

    protected async Task<IList<IMessage>> CallFunctions(IMessage message, ChatModelOptions<ChatCompletionOptions> options, CancellationToken cancellationToken = default)
    {
        var messages = options.Messages;
        messages.Add(message);

        if (message is ModelMessage<string> modelMessage && modelMessage.HasFunctionCalls)
        {
            foreach (var call in modelMessage.FunctionCalls ?? [])
            {
                var logger = Logger.Child($"Tools.{call.Name}");
                logger.Debug(call.Arguments);
                string? content;

                try
                {
                    var args = call.Parse() ?? new Dictionary<string, object?>();
                    var res = await options.Invoke(call, cancellationToken);

                    content = res is string asString ? asString : JsonSerializer.Serialize(res);
                    logger.Debug(content);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                    content = ex.Message;
                }

                messages.Add(new FunctionMessage()
                {
                    FunctionId = call.Id,
                    Content = content
                });
            }
        }

        return messages;
    }
}