using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.AI.Models;
using Microsoft.Teams.AI.Prompts;

namespace Microsoft.Teams.AI.Tests.Utils;

internal class TestChatPrompt : ChatPrompt<TestModelOptions>
{
    private static readonly IChatModel<TestModelOptions> model = new TestModel();

    public TestChatPrompt(ChatPromptOptions? options = null) : base(model, options, NullLogger<TestChatPrompt>.Instance)
    {
        Function(new Function("test-function", "a test function", () => Task.FromResult("test function output")));
    }
}