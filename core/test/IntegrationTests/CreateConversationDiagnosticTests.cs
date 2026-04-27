// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Diagnostic tests exploring CreateConversation parameter combinations.
/// These tests document what the Teams Bot Framework API accepts and rejects,
/// capturing full request/response details including headers.
/// </summary>
public class CreateConversationDiagnosticTests : IClassFixture<IntegrationTestFixture>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;

    public CreateConversationDiagnosticTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
    }

    private async Task<(string first, string? second, string? third)> GetMemberMrisAsync()
    {
        IList<ConversationAccount> members = await _f.ConversationClient.GetConversationMembersAsync(
            _f.ConversationId, _f.ServiceUrl, _f.AgenticIdentity);
        return (
            members[0].Id!,
            members.Count >= 2 ? members[1].Id : null,
            members.Count >= 3 ? members[2].Id : null
        );
    }

    /// <summary>
    /// Sends a CreateConversation request using a raw HttpClient to capture full request/response details.
    /// </summary>
    private async Task<DiagnosticResult> SendDiagnosticRequestAsync(string label, ConversationParameters parameters)
    {
        string url = $"{_f.ServiceUrl.ToString().TrimEnd('/')}/v3/conversations";
        string requestBody = JsonSerializer.Serialize(parameters, JsonOpts);

        // Use the DI-configured HttpClient (has BotAuthenticationHandler for token)
        HttpClient httpClient = _f.ServiceProvider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("BotConversationClient");

        using HttpRequestMessage request = new(HttpMethod.Post, url);
        request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

        _output.WriteLine($"=== {label} ===");
        _output.WriteLine($"POST {url}");
        _output.WriteLine($"Request body:\n{requestBody}");

        using HttpResponseMessage response = await httpClient.SendAsync(request);

        string responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"\nHTTP {(int)response.StatusCode} {response.StatusCode}");

        _output.WriteLine("\nResponse headers:");
        foreach (var header in response.Headers)
        {
            _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
        foreach (var header in response.Content.Headers)
        {
            _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        // Pretty-print JSON response
        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(responseBody);
            
            string pretty = JsonSerializer.Serialize(parsed, JsonOpts);
            _output.WriteLine($"\nResponse body:\n{pretty}");
        }
        catch
        {
            _output.WriteLine($"\nResponse body:\n{responseBody}");
        }

        _output.WriteLine("");

        return new DiagnosticResult
        {
            Label = label,
            StatusCode = (int)response.StatusCode,
            RequestBody = requestBody,
            ResponseBody = responseBody,
            ResponseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
        };
    }

    private record DiagnosticResult
    {
        public required string Label { get; init; }
        public required int StatusCode { get; init; }
        public required string RequestBody { get; init; }
        public required string ResponseBody { get; init; }
        public required Dictionary<string, string> ResponseHeaders { get; init; }
    }

    // =========================================================================
    // 1:1 personal chat — baseline (known working)
    // =========================================================================

    [Fact]
    public async Task PersonalChat_MinimalParams()
    {
        (string memberMri, _, _) = await GetMemberMrisAsync();
        DiagnosticResult result = await SendDiagnosticRequestAsync("1:1 Personal Chat (minimal)", new()
        {
            IsGroup = false,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId
        });
        Assert.True(result.StatusCode is 200 or 201, $"Expected 2xx, got {result.StatusCode}");
    }

    [Fact]
    public async Task PersonalChat_WithBot()
    {
        (string memberMri, _, _) = await GetMemberMrisAsync();
        DiagnosticResult result = await SendDiagnosticRequestAsync("1:1 Personal Chat (with bot)", new()
        {
            IsGroup = false,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId
        });
        Assert.True(result.StatusCode is 200 or 201, $"Expected 2xx, got {result.StatusCode}");
    }

    [Fact]
    public async Task PersonalChat_WithInitialActivity()
    {
        (string memberMri, _, _) = await GetMemberMrisAsync();
        DiagnosticResult result = await SendDiagnosticRequestAsync("1:1 Personal Chat (with activity)", new()
        {
            IsGroup = false,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId,
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", "[Diagnostic] 1:1 with initial activity")
                .Build()
        });
        Assert.True(result.StatusCode is 200 or 201, $"Expected 2xx, got {result.StatusCode}");
    }

    // =========================================================================
    // Group chat variations
    // =========================================================================

    [Fact]
    public async Task GroupChat_TwoMembers_NoBotNoChannelData()
    {
        (string first, string? second, _) = await GetMemberMrisAsync();
        Assert.NotNull(second);
        DiagnosticResult result = await SendDiagnosticRequestAsync("Group Chat: 2 members, no bot, no channelData", new()
        {
            IsGroup = true,
            Members = [new() { Id = first }, new() { Id = second! }],
            TenantId = _f.TenantId
        });
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GroupChat_TwoMembers_WithBot()
    {
        (string first, string? second, _) = await GetMemberMrisAsync();
        Assert.NotNull(second);
        DiagnosticResult result = await SendDiagnosticRequestAsync("Group Chat: 2 members, bot=28:appId", new()
        {
            IsGroup = true,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members = [new() { Id = first }, new() { Id = second! }],
            TenantId = _f.TenantId
        });
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GroupChat_TwoMembers_WithBotAndChannelData()
    {
        (string first, string? second, _) = await GetMemberMrisAsync();
        Assert.NotNull(second);
        DiagnosticResult result = await SendDiagnosticRequestAsync("Group Chat: 2 members, bot, channelData.tenant", new()
        {
            IsGroup = true,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members = [new() { Id = first }, new() { Id = second! }],
            TenantId = _f.TenantId,
            ChannelData = new { tenant = new { id = _f.TenantId } }
        });
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GroupChat_TwoMembers_WithTopicAndActivity()
    {
        (string first, string? second, _) = await GetMemberMrisAsync();
        Assert.NotNull(second);
        DiagnosticResult result = await SendDiagnosticRequestAsync("Group Chat: 2 members, bot, topic, activity, channelData", new()
        {
            IsGroup = true,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members = [new() { Id = first }, new() { Id = second! }],
            TenantId = _f.TenantId,
            TopicName = "Diagnostic group test",
            ChannelData = new { tenant = new { id = _f.TenantId } },
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", "group chat init")
                .Build()
        });
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GroupChat_OneMember_IsGroupTrue()
    {
        (string memberMri, _, _) = await GetMemberMrisAsync();
        DiagnosticResult result = await SendDiagnosticRequestAsync("Group Chat: 1 member, isGroup=true", new()
        {
            IsGroup = true,
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId
        });
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GroupChat_OneMember_WithBot()
    {
        (string memberMri, _, _) = await GetMemberMrisAsync();
        DiagnosticResult result = await SendDiagnosticRequestAsync("Group Chat: 1 member, bot, channelData.tenant", new()
        {
            IsGroup = true,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members = [new() { Id = memberMri }],
            TenantId = _f.TenantId,
            ChannelData = new { tenant = new { id = _f.TenantId } }
        });
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GroupChat_ThreeMembers()
    {
        (string first, string? second, string? third) = await GetMemberMrisAsync();
        Assert.NotNull(second);
        Assert.NotNull(third);
        DiagnosticResult result = await SendDiagnosticRequestAsync("Group Chat: 3 members, bot", new()
        {
            IsGroup = true,
            Bot = new() { Id = $"28:{_f.BotAppId}" },
            Members = [new() { Id = first }, new() { Id = second! }, new() { Id = third! }],
            TenantId = _f.TenantId,
            ChannelData = new { tenant = new { id = _f.TenantId } }
        });
        Assert.Equal(400, result.StatusCode);
    }

    // =========================================================================
    // Channel thread variations
    // =========================================================================

    [Fact]
    public async Task ChannelThread_WithActivity()
    {
        DiagnosticResult result = await SendDiagnosticRequestAsync("Channel Thread: with activity", new()
        {
            IsGroup = true,
            ChannelData = new { channel = new { id = _f.ChannelId } },
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", "[Diagnostic] channel thread")
                .Build(),
            TenantId = _f.TenantId
        });
        Assert.True(result.StatusCode is 200 or 201, $"Expected 2xx, got {result.StatusCode}");
    }

    [Fact]
    public async Task ChannelThread_NoActivity()
    {
        DiagnosticResult result = await SendDiagnosticRequestAsync("Channel Thread: without activity", new()
        {
            IsGroup = true,
            ChannelData = new { channel = new { id = _f.ChannelId } },
            TenantId = _f.TenantId
        });
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ChannelThread_WithMembersAndActivity()
    {
        (string memberMri, _, _) = await GetMemberMrisAsync();
        DiagnosticResult result = await SendDiagnosticRequestAsync("Channel Thread: with members and activity", new()
        {
            IsGroup = true,
            Members = [new() { Id = memberMri }],
            ChannelData = new { channel = new { id = _f.ChannelId } },
            Activity = CoreActivity.CreateBuilder()
                .WithType(ActivityType.Message)
                .WithProperty("text", "[Diagnostic] channel thread with members")
                .Build(),
            TenantId = _f.TenantId
        });
        Assert.True(result.StatusCode is 200 or 201, $"Expected 2xx, got {result.StatusCode}");
    }
}
