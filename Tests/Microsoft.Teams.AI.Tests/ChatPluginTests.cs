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

    [Fact]
    public async Task Test_ChatPrompt_Send_StringWithStreaming()
    {
        // Arrange
        var prompt = new TestChatPrompt();
        var testText = "Hello, world!";
        var onChunk = new OnStreamChunk(async (chunk) => { await Task.CompletedTask; });

        // Act
        var result = await prompt.Send(testText, onChunk);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelMessage<string>>(result);
    }

    [Fact]
    public async Task Test_ChatPrompt_Send_StringWithOptions()
    {
        // Arrange
        var prompt = new TestChatPrompt();
        var testText = "Hello, world!";
        var testOptions = new IChatPrompt<TestModelOptions>.RequestOptions();

        // Act
        var result = await prompt.Send(testText, testOptions);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelMessage<string>>(result);
    }

    [Fact]
    public async Task Test_ChatPrompt_Send_ContentArray()
    {
        // Arrange
        var prompt = new TestChatPrompt();
        var testContent = new IContent[] { new TextContent { Text = "Test content" } };

        // Act
        var result = await prompt.Send(testContent);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelMessage<string>>(result);
    }

    [Fact]
    public async Task Test_ChatPrompt_Send_UserMessageString()
    {
        // Arrange
        var prompt = new TestChatPrompt();
        var testUserMessage = UserMessage.Text("Test user message");

        // Act
        var result = await prompt.Send(testUserMessage);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelMessage<string>>(result);
    }

    [Fact]
    public async Task Test_ChatPrompt_Send_UserMessageContent()
    {
        // Arrange
        var prompt = new TestChatPrompt();
        var testContent = new IContent[] { new TextContent { Text = "Test content" } };
        var testUserMessage = UserMessage.Text(testContent);

        // Act
        var result = await prompt.Send(testUserMessage);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ModelMessage<string>>(result);
    }
}
