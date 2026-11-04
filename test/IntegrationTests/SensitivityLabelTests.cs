// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for sending messages that carry Microsoft Purview sensitivity labels.
/// The Teams service rejects the whole message when a label is malformed, so a successful send verifies the wire shape.
/// Skipped unless <c>TEST_SENSITIVITYLABELID</c> names a label published in the test tenant.
/// </summary>
public class SensitivityLabelTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;
    private readonly ApiClient _api;
    private readonly string? _labelId = Environment.GetEnvironmentVariable("TEST_SENSITIVITYLABELID");

    public SensitivityLabelTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
        _api = _f.ScopedApiClient;
    }

    private async Task SendAsync(MessageActivityInput activity)
    {
        SendActivityResponse? res = await _api.Conversations.CreateActivityAsync(_f.ConversationId, activity);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Created activity: {res.Id}");
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "SensitivityLabels")]
    public async Task SendMessage_WithSensitivityLabel()
    {
        Skip.If(string.IsNullOrEmpty(_labelId), "TEST_SENSITIVITYLABELID not set");

        await SendAsync(new MessageActivityInput()
            .WithText($"[SensitivityLabel.Message] at `{DateTime.UtcNow:s}`")
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = _labelId! }));
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "SensitivityLabels")]
    public async Task SendMessage_WithCitationSensitivityLabel()
    {
        Skip.If(string.IsNullOrEmpty(_labelId), "TEST_SENSITIVITYLABELID not set");

        await SendAsync(new MessageActivityInput()
            .WithText($"[SensitivityLabel.Citation] Cited [1] at `{DateTime.UtcNow:s}`")
            .AddCitation(1, new CitationAppearance
            {
                Name = "Labeled document",
                Abstract = "A cited document with a sensitivity label.",
                SensitivityUsageInfo = new SensitivityUsageInfo { Id = _labelId! },
            }));
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "SensitivityLabels")]
    public async Task SendMessage_WithMessageAndCitationSensitivityLabels()
    {
        Skip.If(string.IsNullOrEmpty(_labelId), "TEST_SENSITIVITYLABELID not set");

        await SendAsync(new MessageActivityInput()
            .WithText($"[SensitivityLabel.Both] Cited [1] at `{DateTime.UtcNow:s}`")
            .AddAIGenerated()
            .AddSensitivityLabel(new SensitivityUsageInfo { Id = _labelId! })
            .AddCitation(1, new CitationAppearance
            {
                Name = "Labeled document",
                Abstract = "A cited document with a sensitivity label.",
                SensitivityUsageInfo = new SensitivityUsageInfo { Id = _labelId! },
            }));
    }
}
