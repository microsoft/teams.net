// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace McpServer;

// MCP clients (and their JSON output schemas) expect snake_case field names,
// so each positional record parameter carries an explicit JsonPropertyName.
// The `property:` target is required: without it, the attribute would land on
// the constructor parameter and System.Text.Json would ignore it.

public sealed record NotifyResult(
    [property: JsonPropertyName("notified")] bool Notified,
    [property: JsonPropertyName("user_id")] string UserId);

public sealed record AskResult(
    [property: JsonPropertyName("request_id")] string RequestId);

public sealed record ReplyResult(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("reply")] string? Reply);

public sealed record ApprovalRequestResult(
    [property: JsonPropertyName("approval_id")] string ApprovalId);

public sealed record ApprovalResult(
    [property: JsonPropertyName("approval_id")] string ApprovalId,
    [property: JsonPropertyName("status")] string Status);

public sealed record UserMatch(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("user_principal_name")] string? UserPrincipalName);

public sealed record FindUserResult(
    [property: JsonPropertyName("matches")] IReadOnlyList<UserMatch> Matches);
