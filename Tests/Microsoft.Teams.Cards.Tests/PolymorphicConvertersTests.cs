using System.Text.Json;

namespace Microsoft.Teams.Cards.Tests;

public class PolymorphicConvertersTests
{
    [Fact]
    public void Serialize_AdaptiveCard_With_PolymorphicBody_And_Actions_Succeeds()
    {
        // Arrange
        var card = new AdaptiveCard(
            [
                new TextBlock("Hello").WithId("t1"),
                new Image("https://example.com/a.png").WithAltText("logo")
            ])
            .WithActions(
                [
                    new OpenUrlAction("https://example.com").WithTitle("Open")
                ]);

        // Act: rely on [JsonConverter] attributes attached to base types
        var json = JsonSerializer.Serialize(card);

        // Assert
        Assert.Contains("\"type\":\"AdaptiveCard\"", json);
        Assert.Contains("\"body\":", json);
        Assert.Contains("\"type\":\"TextBlock\"", json);
        Assert.Contains("\"text\":\"Hello\"", json);
        Assert.Contains("\"type\":\"Image\"", json);
        Assert.Contains("\"url\":\"https://example.com/a.png\"", json);

        Assert.Contains("\"actions\":", json);
        Assert.Contains("\"type\":\"Action.OpenUrl\"", json);
        Assert.Contains("\"url\":\"https://example.com\"", json);
        Assert.Contains("\"title\":\"Open\"", json);
    }

    [Fact]
    public void Serialize_Container_With_Polymorphic_Layouts_Succeeds()
    {
        // Arrange
        var container = new Container(
            [
                new TextBlock("Inside")
            ])
            .WithLayouts(
                [
                    new FlowLayout().WithItemWidth("120px"),
                    new StackLayout()
                ]);

        // Act
        var json = JsonSerializer.Serialize(container);

        // Assert
        Assert.Contains("\"type\":\"Container\"", json);
        Assert.Contains("\"items\":", json);
        Assert.Contains("\"type\":\"TextBlock\"", json);

        Assert.Contains("\"layouts\":", json);
        Assert.Contains("\"type\":\"Layout.Flow\"", json);
        Assert.Contains("\"itemWidth\":\"120px\"", json);
        Assert.Contains("\"type\":\"Layout.Stack\"", json);
    }

    [Fact]
    public void SerializableObject_ToString_Uses_Polymorphic_Converters()
    {
        // Arrange
        var card = new AdaptiveCard(
            [
                new TextBlock("Hello"),
                new Image("https://example.com/x.png")
            ]);

        // Act: ToString() uses System.Text.Json with attributes on base types
        var json = card.ToString();

        // Assert
        Assert.Contains("\"type\": \"AdaptiveCard\"", json);
        Assert.Contains("\"type\": \"TextBlock\"", json);
        Assert.Contains("\"type\": \"Image\"", json);
    }

    [Fact]
    public void Deserialize_CardElement_TextBlock_Succeeds()
    {
        // Arrange
        const string payload = "{\"type\":\"TextBlock\",\"text\":\"Hello\"}";

        var options = new JsonSerializerOptions();
        options.Converters.Add(new CardElementJsonConverter());

        // Act
        var element = JsonSerializer.Deserialize<CardElement>(payload, options);

        // Assert
        Assert.NotNull(element);
        var tb = Assert.IsType<TextBlock>(element);
        Assert.Equal("Hello", tb.Text);
    }

    [Fact]
    public void Deserialize_Action_OpenUrl_Succeeds()
    {
        // Arrange
        const string payload = "{\"type\":\"Action.OpenUrl\",\"url\":\"https://example.com\",\"title\":\"Open\"}";

        var options = new JsonSerializerOptions();
        options.Converters.Add(new ActionJsonConverter());

        // Act
        var action = JsonSerializer.Deserialize<Action>(payload, options);

        // Assert
        Assert.NotNull(action);
        var openUrl = Assert.IsType<OpenUrlAction>(action);
        Assert.Equal("https://example.com", openUrl.Url);
        Assert.Equal("Open", openUrl.Title);
    }

    [Fact]
    public void Deserialize_ContainerLayout_Flow_Succeeds()
    {
        // Arrange
        const string payload = "{\"type\":\"Layout.Flow\",\"itemWidth\":\"120px\"}";

        var options = new JsonSerializerOptions();
        options.Converters.Add(new ContainerLayoutJsonConverter());

        // Act
        var layout = JsonSerializer.Deserialize<ContainerLayout>(payload, options);

        // Assert
        Assert.NotNull(layout);
        var flow = Assert.IsType<FlowLayout>(layout);
        Assert.Equal("120px", flow.ItemWidth);
    }

    [Fact]
    public void Deserialize_AdaptiveCard_With_PolymorphicBody_Actions_And_Layouts_Succeeds()
    {
        // Arrange: full card with body, actions, and nested container + layouts
        const string payload = """
        {
          "type": "AdaptiveCard",
          "body": [
            { "type": "TextBlock", "id": "t1", "text": "Hello" },
            { "type": "Image", "url": "https://example.com/a.png", "altText": "logo" },
            {
              "type": "Container",
              "items": [
                { "type": "TextBlock", "text": "Inside" }
              ],
              "layouts": [
                { "type": "Layout.Flow", "itemWidth": "120px" },
                { "type": "Layout.Stack" }
              ]
            }
          ],
          "actions": [
            { "type": "Action.OpenUrl", "url": "https://example.com", "title": "Open" }
          ]
        }
        """;

        // Act: rely on [JsonConverter] attributes on base types to resolve polymorphic members
        var card = JsonSerializer.Deserialize<AdaptiveCard>(payload);

        // Assert
        Assert.NotNull(card);
        Assert.NotNull(card.Body);
        Assert.True(card.Body.Count >= 3);

        var b0 = card.Body[0];
        var text = Assert.IsType<TextBlock>(b0);
        Assert.Equal("t1", text.Id);
        Assert.Equal("Hello", text.Text);

        var b1 = card.Body[1];
        var image = Assert.IsType<Image>(b1);
        Assert.Equal("https://example.com/a.png", image.Url);
        Assert.Equal("logo", image.AltText);

        var b2 = card.Body[2];
        var container = Assert.IsType<Container>(b2);
        Assert.NotNull(container.Items);
        var innerText = Assert.IsType<TextBlock>(Assert.Single(container.Items));
        Assert.Equal("Inside", innerText.Text);

        Assert.NotNull(container.Layouts);
        Assert.Equal(2, container.Layouts!.Count);
        var flow = Assert.IsType<FlowLayout>(container.Layouts![0]);
        Assert.Equal("120px", flow.ItemWidth);
        Assert.IsType<StackLayout>(container.Layouts![1]);

        Assert.NotNull(card.Actions);
        var openUrl = Assert.IsType<OpenUrlAction>(Assert.Single(card.Actions!));
        Assert.Equal("https://example.com", openUrl.Url);
        Assert.Equal("Open", openUrl.Title);
    }
}