using Microsoft.Extensions.Logging;
using Microsoft.Teams.AI.Models;
using Microsoft.Teams.AI.Prompts;

namespace Microsoft.Teams.AI.Tests.Utils;

internal class TestChatPrompt : ChatPrompt<TestModelOptions>
{
    private static readonly IChatModel<TestModelOptions> model = new TestModel();
    private static readonly ILogger<TestChatPrompt> logger = new LoggerFactory().CreateLogger<TestChatPrompt>();

    public TestChatPrompt(ChatPromptOptions? options = null) : base(model, options, logger)
    {
        Function(new Function("test-function", "a test function", () => Task.FromResult("test function output")));
    }
}