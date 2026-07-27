// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class SearchHandlerTests
{
    [Fact]
    public void InvokeNames_Search_HasExpectedValue()
    {
        Assert.Equal("application/search", InvokeNames.Search);
    }

    [Fact]
    public void Register_SearchRoute_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Invoke, InvokeNames.Search),
            Selector = activity => activity.Name == InvokeNames.Search,
        });

        Assert.Single(router.GetRoutes());
    }

    [Fact]
    public void Search_RouteSelector_MatchesCorrectInvokeName()
    {
        InvokeActivity activity = new(InvokeNames.Search);

        bool matches = activity.Name == InvokeNames.Search;
        Assert.True(matches);
    }

    [Fact]
    public void Search_RouteSelector_DoesNotMatchOtherInvoke()
    {
        InvokeActivity activity = new(InvokeNames.AdaptiveCardAction);

        bool matches = activity.Name == InvokeNames.Search;
        Assert.False(matches);
    }

    [Fact]
    public void Search_InvokeActivity_TypedValueDeserializes()
    {
        var payload = new
        {
            kind = "typeahead",
            queryText = "sea",
            queryOptions = new { skip = 0, top = 5 },
            dataset = "cities"
        };
        InvokeActivity activity = new(InvokeNames.Search)
        {
            Value = JsonSerializer.SerializeToNode(payload)
        };

        InvokeActivity<SearchValue> typed = new(activity);
        SearchValue? value = typed.Value;

        Assert.NotNull(value);
        Assert.Equal("typeahead", value!.Kind);
        Assert.Equal("sea", value.QueryText);
        Assert.Equal("cities", value.Dataset);
        Assert.NotNull(value.QueryOptions);
        Assert.Equal(5, value.QueryOptions!.Top);
    }

    [Fact]
    public void SearchResponse_SerializesToDocumentedShape()
    {
        SearchResponse response = new()
        {
            Value = new SearchResponseValue
            {
                Results = [new SearchResult { Title = "Seattle", Value = "seattle" }]
            }
        };

        JsonNode? node = JsonSerializer.SerializeToNode(response);

        Assert.NotNull(node);
        Assert.Equal(200, node!["statusCode"]!.GetValue<int>());
        Assert.Equal("application/vnd.microsoft.search.searchResponse", node["type"]!.GetValue<string>());
        Assert.Equal("Seattle", node["value"]!["results"]![0]!["title"]!.GetValue<string>());
    }
}
