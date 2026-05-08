// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Teams.Apps.Schema.Entities;

namespace ExtAIBot;

// Parses MCP search results as they are returned by tools and accumulates citation metadata.
// After streaming completes, BuildEntities() returns Teams CitationEntity objects for any
// [N] references that appear in the final response text.
sealed class CitationCollector
{
    private readonly Dictionary<string, CitationEntry> _citations = new();

    public void TryExtract(string result)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(result);
            if (!TryFindResults(doc.RootElement, out JsonElement results)) return;

            foreach (JsonElement item in results.EnumerateArray())
            {
                string? url = GetString(item, "contentUrl") ?? GetString(item, "link");
                if (url is null || _citations.ContainsKey(url)) continue;

                string snippet = GetString(item, "content") ?? GetString(item, "description") ?? "";
                _citations[url] = new CitationEntry(
                    Position: _citations.Count + 1,
                    Url: url,
                    Title: GetString(item, "title") ?? "",
                    Snippet: snippet.Length > 160 ? snippet[..160] : snippet);
            }
        }
        catch { /* not a JSON result with citations */ }
    }

    public IList<Entity> BuildEntities(string fullText)
    {
        HashSet<int> used = [.. Regex.Matches(fullText, @"\[(\d+)\]")
            .Select(m => int.Parse(m.Groups[1].Value))];

        List<CitationClaim> claims = [.. _citations.Values
            .Where(e => used.Contains(e.Position))
            .Select(e => new CitationClaim
            {
                Position = e.Position,
                Appearance = new CitationAppearance
                {
                    Name = string.IsNullOrEmpty(e.Title)
                        ? $"Source {e.Position}"
                        : e.Title[..Math.Min(80, e.Title.Length)],
                    Abstract = string.IsNullOrEmpty(e.Snippet)
                        ? "No description available."
                        : e.Snippet,
                    Url = Uri.TryCreate(e.Url, UriKind.Absolute, out Uri? uri) ? uri : null
                }.ToDocument()
            })];

        return claims.Count == 0
            ? []
            : [new CitationEntity { AdditionalType = ["AIGeneratedContent"], Citation = claims }];
    }

    // MCP InvokeAsync returns a JsonElement of CallToolResult, not the raw server JSON.
    // Results may be at root or nested one level deep (e.g. CallToolResult.structuredContent.results).
    private static bool TryFindResults(JsonElement element, out JsonElement results)
    {
        if (element.TryGetProperty("results", out results) && results.ValueKind == JsonValueKind.Array)
            return true;

        foreach (JsonProperty prop in element.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Object &&
                prop.Value.TryGetProperty("results", out results) &&
                results.ValueKind == JsonValueKind.Array)
                return true;
        }

        results = default;
        return false;
    }

    private static string? GetString(JsonElement el, string property) =>
        el.TryGetProperty(property, out JsonElement v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}

sealed record CitationEntry(int Position, string Url, string Title, string Snippet);
