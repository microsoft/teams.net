using System.Text.Json;

namespace Microsoft.Teams.Cards.Tests;

public class AdaptiveCardFromJsonTests
{
    [Fact]
    public void FromJson_NullOrWhitespace_SetsEmptyBody_And_ReturnsSameInstance()
    {
        var card = new AdaptiveCard([]);

        var result1 = card.FromJson(null!);
        Assert.Same(card, result1);
        Assert.NotNull(card.Body);
        Assert.Empty(card.Body!);

        var result2 = card.FromJson("   \r\n ");
        Assert.Same(card, result2);
        Assert.NotNull(card.Body);
        Assert.Empty(card.Body!);
    }

    [Fact]
    public void FromJson_ArrayOfElements_DeserializesBody()
    {
        const string payload = """
        [
          { "type": "TextBlock", "id": "t1", "text": "Hello" },
          { "type": "Image", "url": "https://example.com/a.png", "altText": "logo" }
        ]
        """;

        var card = new AdaptiveCard([]).FromJson(payload);

        Assert.NotNull(card.Body);
        Assert.Equal(2, card.Body!.Count);

        var tb = Assert.IsType<TextBlock>(card.Body[0]);
        Assert.Equal("t1", tb.Id);
        Assert.Equal("Hello", tb.Text);

        var img = Assert.IsType<Image>(card.Body[1]);
        Assert.Equal("https://example.com/a.png", img.Url);
        Assert.Equal("logo", img.AltText);
    }

    [Fact]
    public void FromJson_CardObjectWithBody_DeserializesBody()
    {
        const string payload = """
        {
          "type": "AdaptiveCard",
          "body": [
            { "type": "TextBlock", "text": "Inside" }
          ]
        }
        """;

        var card = new AdaptiveCard([]).FromJson(payload);

        Assert.NotNull(card.Body);
        var tb = Assert.IsType<TextBlock>(Assert.Single(card.Body!));
        Assert.Equal("Inside", tb.Text);
    }

    [Fact]
    public void FromJson_SingleElementObject_IsWrappedAsBody()
    {
        const string payload = """{ "type": "TextBlock", "text": "Single" }""";

        var card = new AdaptiveCard([]).FromJson(payload);

        Assert.NotNull(card.Body);
        var tb = Assert.IsType<TextBlock>(Assert.Single(card.Body!));
        Assert.Equal("Single", tb.Text);
    }

    [Fact]
    public void FromJson_BodyPropertyNotArray_ThrowsJsonException()
    {
        const string payload = """
        {
          "type": "AdaptiveCard",
          "body": { "type": "TextBlock", "text": "Oops" }
        }
        """;

        var card = new AdaptiveCard([]);
        Assert.Throws<JsonException>(() => card.FromJson(payload));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"not object or array\"")]
    public void FromJson_InvalidRoot_ThrowsJsonException(string payload)
    {
        var card = new AdaptiveCard([]);
        Assert.Throws<JsonException>(() => card.FromJson(payload));
    }
}