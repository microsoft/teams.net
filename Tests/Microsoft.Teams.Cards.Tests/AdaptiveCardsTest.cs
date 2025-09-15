using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards.Tests;

public class AdaptiveCardsTest
{
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Should_Serialize_AdaptiveCard_Simple()
    {
        // arrange
        AdaptiveCard card = new AdaptiveCard()
        {
            Body = new List<CardElement>()
            {
                new TextBlock("Hello, Adaptive Card!")
            }
        };

        // act
        var json = JsonSerializer.Serialize(card, card.GetType(), new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("body", out var bodyElement));
        Assert.Equal(JsonValueKind.Array, bodyElement.ValueKind);
        Assert.Equal(1, bodyElement.GetArrayLength());

        var first = bodyElement[0];
        Assert.Equal("TextBlock", first.GetProperty("type").GetString());
        Assert.Equal("Hello, Adaptive Card!", first.GetProperty("text").GetString());
    }

    [Fact]
    public void Should_Deserialize_AdaptiveCard_Simple()
    {
        string json = @"{
            ""body"": [
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""Hello, Adaptive Card!""
                }
            ]
        }";

        AdaptiveCard card = JsonSerializer.Deserialize<AdaptiveCard>(json, _jsonOptions)!;

        Assert.NotNull(card);
        Assert.Single(card.Body!);
        Assert.IsType<TextBlock>(card.Body![0]);
        Assert.Equal("Hello, Adaptive Card!", ((TextBlock)card.Body[0]).Text);
    }
}