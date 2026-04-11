# Audit Issue 010: O(n²) String Concatenation in Streaming Accumulator

**Severity:** Low (Performance)  
**File:** `core/src/Microsoft.Teams.Bot.Apps/TeamsStreamingWriter.cs`  
**Line:** 92  
**Category:** Memory / performance

---

## Problem

`TeamsStreamingWriter` accumulates streaming chunks by string concatenation:

```csharp
// TeamsStreamingWriter.cs, line 47
private string _accumulated = string.Empty;

// TeamsStreamingWriter.cs, line 92
_accumulated += chunk;
```

In C#, `string` is immutable. Every `+=` operation:
1. Allocates a new `string` on the heap of length `|_accumulated| + |chunk|`.
2. Copies the entire content of both strings into the new allocation.
3. Leaves the old `_accumulated` string unreachable for GC.

For a streaming response with `n` chunks of average size `k`, the total bytes copied is `k + 2k + 3k + ... + nk = O(n²k)`. For a 500-chunk stream with 20-character chunks (a common LLM streaming scenario), this is approximately 2.5 million bytes copied across 500 allocations — all to produce a ~10 KB final string.

In addition, each allocation over 85 KB lands in the Large Object Heap (LOH), which is not compacted by default, increasing memory fragmentation under sustained use.

This is a **performance and memory pressure issue**, not a correctness issue. However, under high concurrency (many simultaneous streaming conversations), it can contribute to GC pressure and latency spikes.

---

## Root Cause

`string +=` was used for simplicity. The standard fix is `System.Text.StringBuilder`.

---

## Suggested Fix Plan

### Step 1 — Replace `_accumulated` with `StringBuilder`

```csharp
// Replace:
private string _accumulated = string.Empty;

// With:
private readonly System.Text.StringBuilder _accumulatedBuilder = new();
```

Update `AppendResponseAsync`:

```csharp
// Replace:
_accumulated += chunk;

// With:
_accumulatedBuilder.Append(chunk);
```

Update all reads of `_accumulated` to use `_accumulatedBuilder.ToString()`. This is called twice:
- In `AppendResponseAsync` when sending an intermediate update: `BuildActivity(_accumulated, StreamType.Streaming)`.
- In `FinalizeResponseAsync` when sending the final message: `BuildActivity(_accumulated, StreamType.Final, ...)`.

```csharp
// Replace _accumulated reads:
BuildActivity(_accumulatedBuilder.ToString(), StreamType.Streaming)
BuildActivity(_accumulatedBuilder.ToString(), StreamType.Final, ...)

// And the empty check:
if (_accumulatedBuilder.Length == 0 && (attachments == null || attachments.Count == 0))
```

### Step 2 — Consider `string.Empty` for zero-content paths

The `BuildActivity` method always materialises the full string. After the fix, `_accumulatedBuilder.ToString()` is O(n) — called at most once per `SendActivityAsync`. This is correct.

### Step 3 — (Optional) Pre-size the builder

If typical chunk sizes and counts are known, pre-size the builder to avoid internal buffer resizing:

```csharp
// Rough capacity: 200 chunks × 50 chars average = 10,000
private readonly System.Text.StringBuilder _accumulatedBuilder = new(capacity: 4096);
```

This is a micro-optimisation and only worthwhile if profiling shows builder resizing as a bottleneck.

---

## Before / After Allocation Comparison

| Metric | Before (`+=`) | After (`StringBuilder`) |
|--------|---------------|------------------------|
| Allocations per chunk | 1 new string (O(n) size) | 0 heap allocations per append |
| Total bytes copied (100 chunks × 100 chars) | ~500,000 | ~10,000 |
| LOH pressure | Possible for large responses | None |
| `ToString()` call on send | None (string already materialised) | 1 per send (O(n) copy, unavoidable) |

---

## Acceptance Criteria

- `_accumulated` field is replaced with `StringBuilder`.
- All three read sites (`AppendResponseAsync` intermediate send, `FinalizeResponseAsync`, and the empty-check in `FinalizeResponseAsync`) use `.ToString()` or `.Length` on the builder.
- Existing streaming integration tests pass.
- (Optional) A benchmark confirms allocation count is O(n) rather than O(n²) for 100-chunk streams.
