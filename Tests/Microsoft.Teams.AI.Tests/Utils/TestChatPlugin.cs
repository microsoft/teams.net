using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Prompts;

namespace Microsoft.Teams.AI.Tests.Utils;

public class TestChatPlugin : IChatPlugin
{
    public virtual Task<object?> OnAfterFunctionCall<TOptions>(IChatPrompt<TOptions> prompt, IFunction function, FunctionCall call, object? output, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(output);
    }

    public virtual Task<IMessage> OnAfterSend<TOptions>(IChatPrompt<TOptions> prompt, IMessage message, TOptions? options = default, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(message);
    }

    public virtual Task<FunctionCall> OnBeforeFunctionCall<TOptions>(IChatPrompt<TOptions> prompt, IFunction function, FunctionCall call, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(call);
    }

    public virtual Task<IMessage> OnBeforeSend<TOptions>(IChatPrompt<TOptions> prompt, IMessage message, TOptions? options = default, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(message);
    }

    public virtual Task<FunctionCollection> OnBuildFunctions<TOptions>(IChatPrompt<TOptions> prompt, FunctionCollection functions, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(functions);
    }

    public virtual Task<DeveloperMessage?> OnBuildInstructions<TOptions>(IChatPrompt<TOptions> prompt, DeveloperMessage? instructions)
    {
        return Task.FromResult(instructions);
    }
}
