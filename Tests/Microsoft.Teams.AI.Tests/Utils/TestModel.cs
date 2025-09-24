using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models;

namespace Microsoft.Teams.AI.Tests.Utils;

internal class TestModel : IChatModel<TestModelOptions>
{
    string IModel<TestModelOptions>.Name => "TestModel";

    Task<ModelMessage<string>> IChatModel<TestModelOptions>.Send(IMessage message, ChatModelOptions<TestModelOptions> options, CancellationToken cancellationToken)
    {
        foreach (var function in options.Functions)
        {
            options.Invoke(new FunctionCall() { Name = function.Name, Id = "testId" }, cancellationToken);
        }

        return _Send(message, options, cancellationToken);
    }

    Task<ModelMessage<string>> IChatModel<TestModelOptions>.Send(IMessage message, ChatModelOptions<TestModelOptions> options, IStream stream, CancellationToken cancellationToken)
    {
        foreach (var function in options.Functions)
        {
            options.Invoke(new FunctionCall() { Name = function.Name, Id = "testId" }, cancellationToken);
        }

        return _Send(message, options, cancellationToken);
    }

    Task<IMessage> IModel<TestModelOptions>.Send(IMessage message, TestModelOptions? options, CancellationToken cancellationToken)
    {
        return Task.FromResult((IMessage)new ModelMessage<string>("test"));
    }

    private Task<ModelMessage<string>> _Send(IMessage message, ChatModelOptions<TestModelOptions> options, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ModelMessage<string>("test"));
    }
}

internal class TestModelOptions { }