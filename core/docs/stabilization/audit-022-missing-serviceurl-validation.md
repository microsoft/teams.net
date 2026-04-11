# Audit Issue 022: Missing `ServiceUrl` Null Validation in `CompatConversations`

**Severity:** Low  
**File:** `core/src/Microsoft.Teams.Bot.Compat/CompatConversations.cs`  
**Lines:** 123, 203  
**Category:** Input validation

---

## Problem

Two methods in `CompatConversations` construct a `Uri` from a nullable `ServiceUrl` property using the null-forgiving operator:

```csharp
// Line 123 — GetActivityMembersWithHttpMessagesAsync
new Uri(ServiceUrl!),

// Line 203 — GetConversationsWithHttpMessagesAsync
new Uri(ServiceUrl!),
```

`ServiceUrl` is a `string?` property that is set externally (e.g., by `CompatAdapter.ContinueConversationAsync`). If it happens to be `null`, the `!` operator suppresses the compiler warning but `new Uri(null!)` throws a `NullReferenceException` (or `ArgumentNullException` in some .NET versions) with no indication that `ServiceUrl` was the problem.

Other methods in the same class (e.g., `SendToConversationWithHttpMessagesAsync`, `ReplyToActivityWithHttpMessagesAsync`) do not access `ServiceUrl` directly because they pass it through fields that are validated elsewhere. These two methods are the exception.

---

## Root Cause

The null-forgiving operator `!` was used instead of an explicit null check. The property is nullable because it's set after construction, creating a window where it can be accessed before initialization.

---

## Suggested Fix

Add explicit validation at the start of each method:

```csharp
public async Task<HttpOperationResponse<IList<ChannelAccount>>> GetActivityMembersWithHttpMessagesAsync(...)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);
    // ... rest of method using new Uri(ServiceUrl)
}

public async Task<HttpOperationResponse<ConversationsResult>> GetConversationsWithHttpMessagesAsync(...)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(ServiceUrl);
    // ... rest of method using new Uri(ServiceUrl)
}
```

---

## Acceptance Criteria

- Null or empty `ServiceUrl` produces a clear `ArgumentException` with the property name, not a `NullReferenceException`.
- Validation is consistent with the pattern used in peer methods in the same class.
