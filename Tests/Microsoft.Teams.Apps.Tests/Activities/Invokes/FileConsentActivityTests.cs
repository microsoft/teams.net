using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities.Invokes;

public class FileConsentActivityTests
{

    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly FileConsentController _controller = new();

    public FileConsentActivityTests()
    {
        _app.AddPlugin(new TestPlugin());
        _app.AddController(_controller);
    }

    //[Fact]
    //public async Task OnFileConsent_ShouldExecute_RegisteredHandler()
    //{
    //    // Arrange
    //    var handlerExecuted = false;

    //    _app.OnFileConsent(async (context) => {
    //        handlerExecuted = true;
    //        return new Response(System.Net.HttpStatusCode.OK);
    //    });

    //    // Act
    //    var activity = CreateTestFileConsentActivity();
    //    var res = await _app.Process<TestPlugin>(_token, activity);

    //    // Assert
    //    Assert.True(handlerExecuted);
    //}

    [Fact]
    public async Task FileConsentAttribute_ShouldRoute_ToControllerMethod()
    {
        // Act
        var activity = CreateTestFileConsentActivity();
        var res = await _app.Process<TestPlugin>(_token, activity);

        // Assert
        Assert.Equal("HandleFileConsent", _controller.MethodCalled);
    }

    [Fact]
    public void FileConsentAttribute_ShouldHaveCorrectNameAndType()
    {
        // Arrange & Act
        var attribute = new FileConsentAttribute();

        // Assert
        Assert.Equal(typeof(FileConsentActivity), attribute.Type);
    }

    //[Fact]
    //public async Task FileConsent_ShouldCoerceContext_ToFileConsentActivity()
    //{
    //    // Arrange
    //     FileConsentActivity? receivedActivity = null;

    //    _app.OnFileConsent(async (context) => {
    //        receivedActivity = context.Activity;
    //        return new Response(System.Net.HttpStatusCode.OK);
    //    });

    //    // Act
    //    var activity = CreateTestFileConsentActivity();
    //    var res = await _app.Process<TestPlugin>(_token, activity);

    //    // Assert
    //    Assert.NotNull(receivedActivity);
    //    Assert.Equal(activity.Id, receivedActivity?.Id);
    //    Assert.Equal(Api.Action.Accept, receivedActivity?.Value?.Action);
    //    Assert.NotNull(receivedActivity?.Value?.Context);
    //}

    private FileConsentActivity CreateTestFileConsentActivity()
    {
        return new FileConsentActivity
        {
            Id = "fileConsentId123",
            ChannelId = new ChannelId("msteams"),
            ServiceUrl = "https://api.botframework.com",
            From = new Account { Id = "userId123", Name = "Test User" },
            Value = new FileConsentCardResponse
            {
                Action = Api.Action.Accept,
                Context = new FileConsentCard
                {
                    Description = "Test file description",
                    SizeInBytes = 1024,
                    AcceptContext = "Test accept context",
                    DeclineContext = "Test decline context"
                },
                UploadInfo = new FileUploadInfo
                {
                    Name = "test-file.txt",
                    UploadUrl = "https://example.com/upload",
                    ContentUrl = "https://example.com/content",
                    UniqueId = "file-123",
                    FileType = "text/plain"
                }
            }
        };
    }

    [TeamsController]
    private class FileConsentController
    {
        public string MethodCalled { get; private set; } = string.Empty;

        [FileConsent]
        public Task<object?> HandleFileConsent(IContext<FileConsentActivity> context)
        {
            MethodCalled = "HandleFileConsent";
            return Task.FromResult<object?>(new Response(System.Net.HttpStatusCode.OK));
        }
    }
}