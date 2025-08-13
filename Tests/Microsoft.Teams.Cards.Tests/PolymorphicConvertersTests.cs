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
    public void Deserialize_CardElement_Throws_NotSupported_When_Converter_Registered()
    {
        // Arrange
        const string payload = "{\"type\":\"TextBlock\",\"text\":\"Hello\"}";

        var options = new JsonSerializerOptions();
        options.Converters.Add(new CardElementJsonConverter());

        // Act + Assert
        var ex = Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Deserialize<CardElement>(payload, options));

        Assert.Contains("Deserializing CardElement is not supported", ex.Message);
    }

    [Fact]
    public void Deserialize_Action_Throws_NotSupported_When_Converter_Registered()
    {
        // Arrange
        const string payload = "{\"type\":\"Action.OpenUrl\",\"url\":\"https://example.com\",\"title\":\"Open\"}";

        var options = new JsonSerializerOptions();
        options.Converters.Add(new ActionJsonConverter());

        // Act + Assert
        var ex = Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Deserialize<Action>(payload, options));

        Assert.Contains("Deserializing Action is not supported", ex.Message);
    }

    [Fact]
    public void Deserialize_ContainerLayout_Throws_NotSupported_When_Converter_Registered()
    {
        // Arrange
        const string payload = "{\"type\":\"Layout.Flow\",\"itemWidth\":\"120px\"}";

        var options = new JsonSerializerOptions();
        options.Converters.Add(new ContainerLayoutJsonConverter());

        // Act + Assert
        var ex = Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Deserialize<ContainerLayout>(payload, options));

        Assert.Contains("Deserializing ContainerLayout is not supported", ex.Message);
    }
}