// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using Xunit.Abstractions;

namespace Microsoft.Bot.Core.Tests;

public class ConversationClientTest
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ConversationClient _conversationClient;
    private readonly Uri _serviceUrl;
    private readonly ITestOutputHelper testOutput;

    public ConversationClientTest(ITestOutputHelper outputHelper)
    {
        testOutput = outputHelper;
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging((builder) => {
            builder.AddXUnit(outputHelper);
            builder.AddFilter("System.Net", LogLevel.Warning);
            builder.AddFilter("Microsoft.Identity", LogLevel.Warning);
            builder.AddFilter("Microsoft.Teams", LogLevel.Information);
        });
        services.AddSingleton(configuration);
        services.AddBotApplication<BotApplication>();
        _serviceProvider = services.BuildServiceProvider();
        _conversationClient = _serviceProvider.GetRequiredService<ConversationClient>();
        _serviceUrl = new Uri(Environment.GetEnvironmentVariable("TEST_SERVICEURL") ?? "https://smba.trafficmanager.net/teams/");
    }

    [Fact]
    public async Task SendActivityDefault()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = GetConversationAccountWithAgenticProperties(),
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };
        SendActivityResponse res = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(res);
        Assert.NotNull(res.Id);
    }

    

    [Fact]
    public async Task SendActivityDefaultWithAgentic()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = GetConversationAccountWithAgenticProperties(),
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };
        SendActivityResponse res = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(res);
        Assert.NotNull(res.Id);
    }


    [Fact]
    public async Task SendActivityToChannel()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = GetConversationAccountWithAgenticProperties(),
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CHANNELID") ?? throw new InvalidOperationException("TEST_CHANNELID environment variable not set")
            }
        };
        SendActivityResponse res = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(res);
        Assert.NotNull(res.Id);
    }

    [Fact]
    public async Task SendActivityToPersonalChat_FailsWithBad_ConversationId()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = GetConversationAccountWithAgenticProperties(),
            Conversation = new()
            {
                Id = "a:1"
            }
        };

        await Assert.ThrowsAsync<HttpRequestException>(()
            => _conversationClient.SendActivityAsync(activity));
    }

    [Fact]
    public async Task UpdateActivity()
    {
        // First send an activity to get an ID
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Original message from Automated tests at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = GetConversationAccountWithAgenticProperties(),
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        // Now update the activity
        CoreActivity updatedActivity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Updated message from Automated tests at `{DateTime.UtcNow:s}`" } },
            From = GetConversationAccountWithAgenticProperties(),
            ServiceUrl = _serviceUrl,
        };

        UpdateActivityResponse updateResponse = await _conversationClient.UpdateActivityAsync(
            activity.Conversation.Id,
            sendResponse.Id,
            updatedActivity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(updateResponse);
        Assert.NotNull(updateResponse.Id);
    }

    [Fact]
    public async Task DeleteActivity()
    {
        // First send an activity to get an ID
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message to delete from Automated tests at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = GetConversationAccountWithAgenticProperties(),
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        // Add a delay for 5 seconds
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Now delete the activity
        await _conversationClient.DeleteActivityAsync(
            activity.Conversation.Id,
            sendResponse.Id,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        // If no exception was thrown, the delete was successful
    }

    [Fact]
    public async Task GetConversationMembers()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        IList<ConversationAccount> members = await _conversationClient.GetConversationMembersAsync(
            conversationId,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        // Log members
        testOutput.WriteLine($"Found {members.Count} members in conversation {conversationId}:");
        foreach (ConversationAccount member in members)
        {
            testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            Assert.NotNull(member);
            Assert.NotNull(member.Id);
        }
    }

    [Fact]
    public async Task GetConversationMember()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        ConversationAccount member = await _conversationClient.GetConversationMemberAsync<ConversationAccount>(
            conversationId,
            userId,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(member);

        // Log member
        testOutput.WriteLine($"Found member in conversation {conversationId}:");
        testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        Assert.NotNull(member);
        Assert.NotNull(member.Id);
    }


    [Fact]
    public async Task GetConversationMembersInChannel()
    {
        string channelId = Environment.GetEnvironmentVariable("TEST_CHANNELID") ?? throw new InvalidOperationException("TEST_CHANNELID environment variable not set");

        IList<ConversationAccount> members = await _conversationClient.GetConversationMembersAsync(
            channelId,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        // Log members
        testOutput.WriteLine($"Found {members.Count} members in channel {channelId}:");
        foreach (ConversationAccount member in members)
        {
            testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            Assert.NotNull(member);
            Assert.NotNull(member.Id);
        }
    }

    [Fact]
    public async Task GetActivityMembers()
    {
        // First send an activity to get an activity ID
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message for GetActivityMembers test at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = GetConversationAccountWithAgenticProperties(),
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        // Now get the members of this activity
        IList<ConversationAccount> members = await _conversationClient.GetActivityMembersAsync(
            activity.Conversation.Id,
            sendResponse.Id,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        // Log activity members
        testOutput.WriteLine($"Found {members.Count} members for activity {sendResponse.Id}:");
        foreach (ConversationAccount member in members)
        {
            testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            Assert.NotNull(member);
            Assert.NotNull(member.Id);
        }
    }

    // TODO: This doesn't work
    [Fact(Skip = "Method not allowed by API")]
    public async Task GetConversations()
    {
        GetConversationsResponse response = await _conversationClient.GetConversationsAsync(
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Conversations);
        Assert.NotEmpty(response.Conversations);

        // Log conversations
        testOutput.WriteLine($"Found {response.Conversations.Count} conversations:");
        foreach (ConversationMembers conversation in response.Conversations)
        {
            testOutput.WriteLine($"  - Conversation Id: {conversation.Id}");
            Assert.NotNull(conversation);
            Assert.NotNull(conversation.Id);

            if (conversation.Members != null && conversation.Members.Any())
            {
                testOutput.WriteLine($"    Members ({conversation.Members.Count}):");
                foreach (ConversationAccount member in conversation.Members)
                {
                    testOutput.WriteLine($"      - Id: {member.Id}, Name: {member.Name}");
                }
            }
        }
    }

    [Fact]
    public async Task CreateConversation_WithMembers()
    {
        // Create a 1-on-1 conversation with a member
        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members =
            [
                new()
                {
                    Id = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set"),
                }
            ],
            // TODO: This is required for some reason. Should it be required in the api?
            TenantId = Environment.GetEnvironmentVariable("AzureAd__TenantId") ?? throw new InvalidOperationException("AzureAd__TenantId environment variable not set")
        };

        CreateConversationResponse response = await _conversationClient.CreateConversationAsync(
            parameters,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        testOutput.WriteLine($"Created conversation: {response.Id}");
        testOutput.WriteLine($"  ActivityId: {response.ActivityId}");
        testOutput.WriteLine($"  ServiceUrl: {response.ServiceUrl}");

        // Send a message to the newly created conversation
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Test message to new conversation at {DateTime.UtcNow:s}" } },
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = response.Id
            }
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        testOutput.WriteLine($"  Sent message with activity ID: {sendResponse.Id}");
    }

    // TODO: This doesn't work
    [Fact(Skip = "Incorrect conversation creation parameters")]
    public async Task CreateConversation_WithGroup()
    {
        // Create a group conversation
        ConversationParameters parameters = new()
        {
            IsGroup = true,
            Members =
            [
                new()
                {
                    Id = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set"),
                },
                new()
                {
                    Id = Environment.GetEnvironmentVariable("TEST_USER_ID_2") ?? throw new InvalidOperationException("TEST_USER_ID_2 environment variable not set"),
                }
            ],
            TenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? throw new InvalidOperationException("TENANT_ID environment variable not set")
        };

        CreateConversationResponse response = await _conversationClient.CreateConversationAsync(
            parameters,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        testOutput.WriteLine($"Created group conversation: {response.Id}");

        // Send a message to the newly created group conversation
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Test message to new group conversation at {DateTime.UtcNow:s}" } },
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = response.Id
            }
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        testOutput.WriteLine($"  Sent message with activity ID: {sendResponse.Id}");
    }

    // TODO: This doesn't work
    [Fact(Skip = "Incorrect conversation creation parameters")]
    public async Task CreateConversation_WithTopicName()
    {
        // Create a conversation with a topic name
        ConversationParameters parameters = new()
        {
            IsGroup = true,
            TopicName = $"Test Conversation - {DateTime.UtcNow:s}",
            Members =
            [
                new()
                {
                    Id = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set"),
                }
            ],
            TenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? throw new InvalidOperationException("TENANT_ID environment variable not set")
        };

        CreateConversationResponse response = await _conversationClient.CreateConversationAsync(
            parameters,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        testOutput.WriteLine($"Created conversation with topic '{parameters.TopicName}': {response.Id}");

        // Send a message to the newly created conversation
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Test message to conversation with topic name at {DateTime.UtcNow:s}" } },
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = response.Id
            }
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        testOutput.WriteLine($"  Sent message with activity ID: {sendResponse.Id}");
    }

    // TODO: This doesn't fail, but doesn't actually create the initial activity
    [Fact]
    public async Task CreateConversation_WithInitialActivity()
    {
        // Create a conversation with an initial message
        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members =
            [
                new()
                {
                    Id = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set"),
                }
            ],
            Activity = new CoreActivity
            {
                Type = ActivityType.Message,
                Properties = { { "text", $"Initial message sent at {DateTime.UtcNow:s}" } },
            },
            TenantId = Environment.GetEnvironmentVariable("AzureAd__TenantId") ?? throw new InvalidOperationException("AzureAd__TenantId environment variable not set")
        };

        CreateConversationResponse response = await _conversationClient.CreateConversationAsync(
            parameters,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        // Assert.NotNull(response.ActivityId); // Should have an activity ID since we sent an initial message

        testOutput.WriteLine($"Created conversation with initial activity: {response.Id}");
        testOutput.WriteLine($"  Initial activity ID: {response.ActivityId}");
    }

    [Fact]
    public async Task CreateConversation_WithChannelData()
    {
        // Create a conversation with channel-specific data
        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members =
            [
                new()
                {
                    Id = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set"),
                }
            ],
            ChannelData = new
            {
                teamsChannelId = Environment.GetEnvironmentVariable("TEST_CHANNELID")
            },
            TenantId = Environment.GetEnvironmentVariable("AzureAd__TenantId") ?? throw new InvalidOperationException("AzureAd__TenantId environment variable not set")
        };

        CreateConversationResponse response = await _conversationClient.CreateConversationAsync(
            parameters,
            _serviceUrl,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        testOutput.WriteLine($"Created conversation with channel data: {response.Id}");
    }

    [Fact]
    public async Task GetConversationPagedMembers()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        PagedMembersResult result = await _conversationClient.GetConversationPagedMembersAsync(
            conversationId,
            _serviceUrl,
            10,
            null!,
            GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        testOutput.WriteLine($"Found {result.Members.Count} members in page:");
        foreach (ConversationAccount member in result.Members)
        {
            testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            Assert.NotNull(member);
            Assert.NotNull(member.Id);
        }

        if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
        {
            testOutput.WriteLine($"Continuation token: {result.ContinuationToken}");
        }
    }

    [Fact(Skip = "PageSize parameter not respected by API")]
    public async Task GetConversationPagedMembers_WithPageSize()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        PagedMembersResult result = await _conversationClient.GetConversationPagedMembersAsync(
            conversationId,
            _serviceUrl,
            pageSize: 1,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);
        Assert.Single(result.Members);

        testOutput.WriteLine($"Found {result.Members.Count} members with pageSize=1:");
        foreach (ConversationAccount member in result.Members)
        {
            testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }

        // If there's a continuation token, get the next page
        if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
        {
            testOutput.WriteLine($"Getting next page with continuation token...");

            PagedMembersResult nextPage = await _conversationClient.GetConversationPagedMembersAsync(
                conversationId,
                _serviceUrl,
                pageSize: 1,
                continuationToken: result.ContinuationToken,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(nextPage);
            Assert.NotNull(nextPage.Members);

            testOutput.WriteLine($"Found {nextPage.Members.Count} members in next page:");
            foreach (ConversationAccount member in nextPage.Members)
            {
                testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            }
        }
    }

    [Fact(Skip = "Method not allowed by API")]
    public async Task DeleteConversationMember()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        // Get members before deletion
        IList<ConversationAccount> membersBefore = await _conversationClient.GetConversationMembersAsync(
            conversationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(membersBefore);
        Assert.NotEmpty(membersBefore);

        testOutput.WriteLine($"Members before deletion: {membersBefore.Count}");
        foreach (ConversationAccount member in membersBefore)
        {
            testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }

        // Delete the test user
        string memberToDelete = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        // Verify the member is in the conversation before attempting to delete
        Assert.Contains(membersBefore, m => m.Id == memberToDelete);

        await _conversationClient.DeleteConversationMemberAsync(
            conversationId,
            memberToDelete,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        testOutput.WriteLine($"Deleted member: {memberToDelete}");

        // Get members after deletion
        IList<ConversationAccount> membersAfter = await _conversationClient.GetConversationMembersAsync(
            conversationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(membersAfter);

        testOutput.WriteLine($"Members after deletion: {membersAfter.Count}");
        foreach (ConversationAccount member in membersAfter)
        {
            testOutput.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }

        // Verify the member was deleted
        Assert.DoesNotContain(membersAfter, m => m.Id == memberToDelete);
    }

    [Fact(Skip = "Unknown activity type error")]
    public async Task SendConversationHistory()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        // Create a transcript with historic activities
        Transcript transcript = new()
        {
            Activities =
            [
                new()
                {
                    Type = ActivityType.Message,
                    Id = Guid.NewGuid().ToString(),
                    Properties = { { "text", "Historic message 1" } },
                    ServiceUrl = _serviceUrl,
                    Conversation = new() { Id = conversationId }
                },
                new()
                {
                    Type = ActivityType.Message,
                    Id = Guid.NewGuid().ToString(),
                    Properties = { { "text", "Historic message 2" } },
                    ServiceUrl = _serviceUrl,
                    Conversation = new() { Id = conversationId }
                },
                new()
                {
                    Type = ActivityType.Message,
                    Id = Guid.NewGuid().ToString(),
                    Properties = { { "text", "Historic message 3" } },
                    ServiceUrl = _serviceUrl,
                    Conversation = new() { Id = conversationId }
                }
            ]
        };

        SendConversationHistoryResponse response = await _conversationClient.SendConversationHistoryAsync(
            conversationId,
            transcript,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);

        testOutput.WriteLine($"Sent conversation history with {transcript.Activities?.Count} activities");
        testOutput.WriteLine($"Response ID: {response.Id}");
    }

    [Fact(Skip = "Attachment upload endpoint not found")]
    public async Task UploadAttachment()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        // Create a simple text file as an attachment
        string fileContent = "This is a test attachment file created at " + DateTime.UtcNow.ToString("s");
        byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);

        AttachmentData attachmentData = new()
        {
            Type = "text/plain",
            Name = "test-attachment.txt",
            OriginalBase64 = fileBytes
        };

        UploadAttachmentResponse response = await _conversationClient.UploadAttachmentAsync(
            conversationId,
            attachmentData,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        testOutput.WriteLine($"Uploaded attachment: {attachmentData.Name}");
        testOutput.WriteLine($"  Attachment ID: {response.Id}");
        testOutput.WriteLine($"  Content-Type: {attachmentData.Type}");
        testOutput.WriteLine($"  Size: {fileBytes.Length} bytes");
    }

    private ConversationAccount GetConversationAccountWithAgenticProperties()
    {
        string agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");
        string agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");
        string agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId");

        if (string.IsNullOrEmpty(agenticUserId))
        {
            return new ConversationAccount();
        }

        ConversationAccount account = new()
        {
            Id = agenticUserId,
            Name = "Agentic User",
            Properties =
            {
                { "agenticUserId", agenticUserId },
                { "agenticAppId", agenticAppId },
                { "agenticAppBlueprintId", agenticAppBlueprintId }
            }
        };
        return account;
    }

    private AgenticIdentity GetAgenticIdentity()
    {
        string agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");
        string agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");
        string agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId");

        if (string.IsNullOrEmpty(agenticUserId))
        {
            return null!;
        }    

        AgenticIdentity identity = new()
        {
            AgenticUserId = agenticUserId,
            AgenticAppId = agenticAppId,
            AgenticAppBlueprintId = agenticAppBlueprintId
        };
        return identity;
    }
}
