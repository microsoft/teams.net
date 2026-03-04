// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace TabApp;

public class PostToChatBody
{
    public required string Message { get; set; }
    public string? ChatId { get; set; }
    public string? ChannelId { get; set; }
}

public record PostToChatResult(bool Ok);
