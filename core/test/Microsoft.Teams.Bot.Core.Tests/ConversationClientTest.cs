// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Connector;
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

    private readonly string _conversationId;
    private readonly ConversationAccount _recipient = new ConversationAccount();
    private AgenticIdentity? _agenticIdentity;

    public ConversationClientTest(ITestOutputHelper outputHelper)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging((builder) => {
            builder.AddXUnit(outputHelper);
            builder.AddFilter("System.Net", LogLevel.Warning);
            builder.AddFilter("Microsoft.Identity", LogLevel.Error);
            builder.AddFilter("Microsoft.Teams", LogLevel.Information);
        });
        services.AddSingleton(configuration);
        services.AddBotApplication<BotApplication>();
        _serviceProvider = services.BuildServiceProvider();
        _conversationClient = _serviceProvider.GetRequiredService<ConversationClient>();
        _serviceUrl = new Uri(Environment.GetEnvironmentVariable("TEST_SERVICEURL") ?? "https://smba.trafficmanager.net/teams/");
        _conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");
        string agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId") ?? throw new InvalidOperationException("AzureAd__ClientId environment variable not set");
        string? agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");// ?? throw new InvalidOperationException("TEST_AGENTIC_APPID environment variable not set");
        string? agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");// ?? throw new InvalidOperationException("TEST_AGENTIC_USERID environment variable not set");

        _agenticIdentity = null;
        if (!string.IsNullOrEmpty(agenticAppId) && !string.IsNullOrEmpty(agenticUserId))
        {
            _recipient.Properties.Add("agenticAppBlueprintId", agenticAppBlueprintId);
            _recipient.Properties.Add("agenticAppId", agenticAppId);
            _recipient.Properties.Add("agenticUserId", agenticUserId);
            _agenticIdentity = AgenticIdentity.FromProperties(_recipient.Properties);
        }
    }

    [Fact]
    public async Task SendActivityToChannel()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
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
            Conversation = new("a:1"),
            From = _recipient
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
            Conversation = new(_conversationId),
            From = _recipient
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        // Now update the activity
        CoreActivity updatedActivity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Updated message from Automated tests at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
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
            Conversation = new(_conversationId),
            From = _recipient
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
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        // If no exception was thrown, the delete was successful
    }

    [Fact]
    public async Task GetConversationMembers()
    {
        IList<ConversationAccount> members = await _conversationClient.GetConversationMembersAsync(
            _conversationId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        // Log members
        Console.WriteLine($"Found {members.Count} members in conversation {_conversationId}:");
        foreach (ConversationAccount member in members)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            Assert.NotNull(member);
            Assert.NotNull(member.Id);
        }
    }

    [Fact]
    public async Task GetConversationMember()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        ConversationAccount member = await _conversationClient.GetConversationMemberAsync<ConversationAccount>(
            _conversationId,
            userId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(member);

        // Log member
        Console.WriteLine($"Found member in conversation {_conversationId}:");
        Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
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
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        // Log members
        Console.WriteLine($"Found {members.Count} members in channel {channelId}:");
        foreach (ConversationAccount member in members)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
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
            Conversation = new(_conversationId),
            From = _recipient
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        // Now get the members of this activity
        IList<ConversationAccount> members = await _conversationClient.GetActivityMembersAsync(
            activity.Conversation.Id,
            sendResponse.Id,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        // Log activity members
        Console.WriteLine($"Found {members.Count} members for activity {sendResponse.Id}:");
        foreach (ConversationAccount member in members)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
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
        Console.WriteLine($"Found {response.Conversations.Count} conversations:");
        foreach (ConversationMembers conversation in response.Conversations)
        {
            Console.WriteLine($"  - Conversation Id: {conversation.Id}");
            Assert.NotNull(conversation);
            Assert.NotNull(conversation.Id);

            if (conversation.Members != null && conversation.Members.Any())
            {
                Console.WriteLine($"    Members ({conversation.Members.Count}):");
                foreach (ConversationAccount member in conversation.Members)
                {
                    Console.WriteLine($"      - Id: {member.Id}, Name: {member.Name}");
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
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        Console.WriteLine($"Created conversation: {response.Id}");
        Console.WriteLine($"  ActivityId: {response.ActivityId}");
        Console.WriteLine($"  ServiceUrl: {response.ServiceUrl}");

        // Send a message to the newly created conversation
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Test message to new conversation at {DateTime.UtcNow:s}" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(response.Id)
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        Console.WriteLine($"  Sent message with activity ID: {sendResponse.Id}");
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
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        Console.WriteLine($"Created group conversation: {response.Id}");

        // Send a message to the newly created group conversation
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Test message to new group conversation at {DateTime.UtcNow:s}" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(response.Id)
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        Console.WriteLine($"  Sent message with activity ID: {sendResponse.Id}");
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

        Console.WriteLine($"Created conversation with topic '{parameters.TopicName}': {response.Id}");

        // Send a message to the newly created conversation
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Test message to conversation with topic name at {DateTime.UtcNow:s}" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(response.Id)
        };

        SendActivityResponse sendResponse = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        Console.WriteLine($"  Sent message with activity ID: {sendResponse.Id}");
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
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        // Assert.NotNull(response.ActivityId); // Should have an activity ID since we sent an initial message

        Console.WriteLine($"Created conversation with initial activity: {response.Id}");
        Console.WriteLine($"  Initial activity ID: {response.ActivityId}");
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
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        Console.WriteLine($"Created conversation with channel data: {response.Id}");
    }

    [Fact]
    public async Task GetConversationPagedMembers()
    {
        PagedMembersResult result = await _conversationClient.GetConversationPagedMembersAsync(
            _conversationId,
            _serviceUrl,
            5,
            null,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        Console.WriteLine($"Found {result.Members.Count} members in page:");
        foreach (ConversationAccount member in result.Members)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            Assert.NotNull(member);
            Assert.NotNull(member.Id);
        }

        if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
        {
            Console.WriteLine($"Continuation token: {result.ContinuationToken}");
        }
    }

    [Fact]
    public async Task AddRemoveReactionsToChat_Default()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"I'm going to add and remove reactions from this message." } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
        };
        SendActivityResponse res = await _conversationClient.SendActivityAsync(activity, cancellationToken: CancellationToken.None);
        Assert.NotNull(res);
        Assert.NotNull(res.Id);

        await _conversationClient.AddReactionAsync(_conversationId, res.Id, "laugh", _serviceUrl, _agenticIdentity);
        await Task.Delay(500);
        await _conversationClient.AddReactionAsync(_conversationId, res.Id, "sad", _serviceUrl, _agenticIdentity);
        await Task.Delay(500);
        await _conversationClient.AddReactionAsync(_conversationId, res.Id, "yes-tone4", _serviceUrl, _agenticIdentity);

        await Task.Delay(500);
        await _conversationClient.DeleteReactionAsync(_conversationId, res.Id, "yes-tone4", _serviceUrl, _agenticIdentity);
        await Task.Delay(500);
        await _conversationClient.DeleteReactionAsync(_conversationId, res.Id, "sad", _serviceUrl, _agenticIdentity);
    }

    [Fact(Skip = "PageSize parameter not respected by API")]
    public async Task GetConversationPagedMembers_WithPageSize()
    {
        PagedMembersResult result = await _conversationClient.GetConversationPagedMembersAsync(
            _conversationId,
            _serviceUrl,
            pageSize: 1,
            agenticIdentity: _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);
        Assert.Single(result.Members);

        Console.WriteLine($"Found {result.Members.Count} members with pageSize=1:");
        foreach (ConversationAccount member in result.Members)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }

        // If there's a continuation token, get the next page
        if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
        {
            Console.WriteLine($"Getting next page with continuation token...");

            PagedMembersResult nextPage = await _conversationClient.GetConversationPagedMembersAsync(
                _conversationId,
                _serviceUrl,
                pageSize: 1,
                continuationToken: result.ContinuationToken,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(nextPage);
            Assert.NotNull(nextPage.Members);

            Console.WriteLine($"Found {nextPage.Members.Count} members in next page:");
            foreach (ConversationAccount member in nextPage.Members)
            {
                Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
            }
        }
    }

    [Fact(Skip = "Method not allowed by API")]
    public async Task DeleteConversationMember()
    {
        // Get members before deletion
        IList<ConversationAccount> membersBefore = await _conversationClient.GetConversationMembersAsync(
            _conversationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(membersBefore);
        Assert.NotEmpty(membersBefore);

        Console.WriteLine($"Members before deletion: {membersBefore.Count}");
        foreach (ConversationAccount member in membersBefore)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }

        // Delete the test user
        string memberToDelete = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        // Verify the member is in the conversation before attempting to delete
        Assert.Contains(membersBefore, m => m.Id == memberToDelete);

        await _conversationClient.DeleteConversationMemberAsync(
            _conversationId,
            memberToDelete,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"Deleted member: {memberToDelete}");

        // Get members after deletion
        IList<ConversationAccount> membersAfter = await _conversationClient.GetConversationMembersAsync(
            _conversationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(membersAfter);

        Console.WriteLine($"Members after deletion: {membersAfter.Count}");
        foreach (ConversationAccount member in membersAfter)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }

        // Verify the member was deleted
        Assert.DoesNotContain(membersAfter, m => m.Id == memberToDelete);
    }

    [Fact(Skip = "Unknown activity type error")]
    public async Task SendConversationHistory()
    {
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
                    Conversation = new(_conversationId)
                },
                new()
                {
                    Type = ActivityType.Message,
                    Id = Guid.NewGuid().ToString(),
                    Properties = { { "text", "Historic message 2" } },
                    ServiceUrl = _serviceUrl,
                    Conversation = new(_conversationId)
                },
                new()
                {
                    Type = ActivityType.Message,
                    Id = Guid.NewGuid().ToString(),
                    Properties = { { "text", "Historic message 3" } },
                    ServiceUrl = _serviceUrl,
                    Conversation = new(_conversationId)
                }
            ]
        };

        SendConversationHistoryResponse response = await _conversationClient.SendConversationHistoryAsync(
            _conversationId,
            transcript,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);

        Console.WriteLine($"Sent conversation history with {transcript.Activities?.Count} activities");
        Console.WriteLine($"Response ID: {response.Id}");
    }

    [Fact(Skip = "Attachment upload endpoint not found")]
    public async Task UploadAttachment()
    {
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
            _conversationId,
            attachmentData,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        Console.WriteLine($"Uploaded attachment: {attachmentData.Name}");
        Console.WriteLine($"  Attachment ID: {response.Id}");
        Console.WriteLine($"  Content-Type: {attachmentData.Type}");
        Console.WriteLine($"  Size: {fileBytes.Length} bytes");
    }
}
