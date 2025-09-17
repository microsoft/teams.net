using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.AI.Tests.Utils;

using Moq;

namespace Microsoft.Teams.AI.Tests;

public class ChatPluginTests
{
    [Theory]
    [InlineData("OnBeforeSend")]
    [InlineData("OnAfterSend")]
    [InlineData("OnBeforeFunctionCall")]
    [InlineData("OnAfterFunctionCall")]
    [InlineData("OnBuildFunctions")]
    [InlineData("OnBuildInstructions")]
    public async Task Test_ChatPlugin_HooksCalled(string hookName)
    {
        // Arrange
        var chatPlugin = new Mock<TestChatPlugin>() { CallBase = true };
        var prompt = new TestChatPrompt();
        prompt.Plugin(chatPlugin.Object);

        var message = UserMessage.Text("Hello");
        var options = new TestModelOptions();

        // Act
        var result = await prompt.Send(message, new());

        // Assert
        Assert.NotNull(result);
        
        switch (hookName)
        {
            case "OnBeforeSend":
                chatPlugin.Verify(p => p.OnBeforeSend(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<IMessage>(), It.IsAny<TestModelOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case "OnAfterSend":
                chatPlugin.Verify(p => p.OnAfterSend(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<IMessage>(), It.IsAny<TestModelOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case "OnBeforeFunctionCall":
                chatPlugin.Verify(p => p.OnBeforeFunctionCall(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<IFunction>(), It.IsAny<FunctionCall>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case "OnAfterFunctionCall":
                chatPlugin.Verify(p => p.OnAfterFunctionCall(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<IFunction>(), It.IsAny<FunctionCall>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case "OnBuildFunctions":
                chatPlugin.Verify(p => p.OnBuildFunctions(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<FunctionCollection>(), It.IsAny<CancellationToken>()), Times.Once);
                break;
            case "OnBuildInstructions":
                chatPlugin.Verify(p => p.OnBuildInstructions(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<DeveloperMessage?>()), Times.Once);
                break;
        }

    }

    [Fact]
    public async Task Test_ChatPlugin_OnBuildFunctions_AddFunction()
    {
        // Arrange
        var testFunctionInvoked = false;
        var chatPlugin = new Mock<TestChatPlugin>() { CallBase = true };
        chatPlugin.Setup(p => p.OnBuildFunctions(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<FunctionCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IChatPrompt<TestModelOptions> prompt, FunctionCollection functions, CancellationToken cancellationToken) =>
            {
                var newFunction = new Function("injected function", "a test function", () => testFunctionInvoked = true);
                functions.Add(newFunction);
                return functions;
            });

        var prompt = new TestChatPrompt();
        prompt.Plugin(chatPlugin.Object);

        var message = UserMessage.Text("Hello");
        var options = new TestModelOptions();

        // Act
        var result = await prompt.Send(message, new());

        // Assert
        Assert.NotNull(result);
        chatPlugin.Verify(p => p.OnBuildFunctions(It.IsAny<IChatPrompt<TestModelOptions>>(), It.IsAny<FunctionCollection>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(testFunctionInvoked, "The injected function should have been invoked.");
        
        // injected function does not persist in the prompt's function collection
        Assert.False(prompt.Functions.Has("injected function"));
    }
}
