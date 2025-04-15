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
        if (toolCallUpdate.ToolCallId != null)
        {
            _indexToToolCallId[toolCallUpdate.Index] = toolCallUpdate.ToolCallId;
        }

        // Keep track of which function name belongs to this update index.
        if (toolCallUpdate.FunctionName != null)
        {
            _indexToFunctionName[toolCallUpdate.Index] = toolCallUpdate.FunctionName;
        }

        // Keep track of which function arguments belong to this update index,
        // and accumulate the arguments as new updates arrive.
        if (toolCallUpdate.FunctionArgumentsUpdate != null && !toolCallUpdate.FunctionArgumentsUpdate.ToMemory().IsEmpty)
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

        foreach ((int index, string toolCallId) in _indexToToolCallId)
        {
            ReadOnlySequence<byte> sequence = _indexToFunctionArguments[index].Build();

            ChatToolCall toolCall = ChatToolCall.CreateFunctionToolCall(
                id: toolCallId,
                functionName: _indexToFunctionName[index],
                functionArguments: BinaryData.FromBytes(sequence.ToArray()));

            toolCalls.Add(toolCall);
        }

        return toolCalls;
    }
}