// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI.Builders;

/// <summary>
/// https://github.com/openai/openai-dotnet/blob/main/examples/Chat/Example04_FunctionCallingStreaming.cs#L18
/// </summary>
public class StreamingChatToolCallsBuilder
{
    private readonly Dictionary<int, string> _indexToToolCallId = [];
    private readonly Dictionary<int, string> _indexToFunctionName = [];
    private readonly Dictionary<int, SequenceBuilder<byte>> _indexToFunctionArguments = [];

    public void Append(StreamingChatToolCallUpdate toolCallUpdate)
    {
        // Keep track of which tool call ID belongs to this update index.
        if (toolCallUpdate.ToolCallId is not null)
        {
            _indexToToolCallId[toolCallUpdate.Index] = toolCallUpdate.ToolCallId;
        }

        // Keep track of which function name belongs to this update index.
        if (toolCallUpdate.FunctionName is not null)
        {
            _indexToFunctionName[toolCallUpdate.Index] = toolCallUpdate.FunctionName;
        }

        // Keep track of which function arguments belong to this update index,
        // and accumulate the arguments as new updates arrive.
        if (toolCallUpdate.FunctionArgumentsUpdate is not null && !toolCallUpdate.FunctionArgumentsUpdate.ToMemory().IsEmpty)
        {
            if (!_indexToFunctionArguments.TryGetValue(toolCallUpdate.Index, out SequenceBuilder<byte>? argumentsBuilder))
            {
                argumentsBuilder = new SequenceBuilder<byte>();
                _indexToFunctionArguments[toolCallUpdate.Index] = argumentsBuilder;
            }

            argumentsBuilder.Append(toolCallUpdate.FunctionArgumentsUpdate);
        }
    }

    public IReadOnlyList<ChatToolCall> Build()
    {
        List<ChatToolCall> toolCalls = [];

        foreach (var kv in _indexToToolCallId)
        {
            ReadOnlySequence<byte> sequence = _indexToFunctionArguments[kv.Key].Build();

            ChatToolCall toolCall = ChatToolCall.CreateFunctionToolCall(
                id: kv.Value,
                functionName: _indexToFunctionName[kv.Key],
                functionArguments: BinaryData.FromBytes(sequence.ToArray()));

            toolCalls.Add(toolCall);
        }

        return toolCalls;
    }
}