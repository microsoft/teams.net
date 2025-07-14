using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Teams.Cards.Tests
{
    public class AdaptiveCardComponentsTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AdaptiveCard SetupAdaptiveCardWithGraphComponents()
        {
            var body = new List<CardElement>
            {
                new TextBlock("Microsoft Graph Components")
                    .WithWeight(TextWeight.Bolder)
                    .WithSize(TextSize.Large)
                    .WithColor(TextColor.Accent),

                new ComUserMicrosoftGraphComponent()
                    .WithId("userComponent")
                    .WithProperties(new PersonaProperties()
                        .WithUserPrincipalName("user@contoso.com")),

                new ComUsersMicrosoftGraphComponent()
                    .WithId("usersComponent")
                    .WithProperties(new PersonaSetProperties()
                        .WithUsers(new List<PersonaProperties>
                        {
                            new PersonaProperties().WithUserPrincipalName("user1@contoso.com"),
                            new PersonaProperties().WithUserPrincipalName("user2@contoso.com")
                        })),

                new ComResourceMicrosoftGraphComponent()
                    .WithId("resourceComponent")
                    .WithProperties(new ResourceProperties()
                        .WithId("resource-id-123")
                        .WithResourceReference(new Dictionary<string, string> { ["type"] = "channel" })),

                new ComFileMicrosoftGraphComponent()
                    .WithId("fileComponent")
                    .WithProperties(new FileProperties()
                        .WithName("file-id-456")),

                new ComEventMicrosoftGraphComponent()
                    .WithId("eventComponent")
                    .WithProperties(new CalendarEventProperties()
                        .WithId("event-id-789"))
            };

            var card = new AdaptiveCard().WithVersion(Version.Version1_5).WithId("graphComponentsCard")
                .WithSchema("https://adaptivecards.io/schemas/adaptive-card.json")
                .WithBody(body);
            return card;
        }

        [Fact]
        public void GraphComponents_JsonSerialize()
        {
            var card = SetupAdaptiveCardWithGraphComponents();
            var expectedJson = File.ReadAllText(@"../../../Json/GraphComponents.json");

            var json = JsonSerializer.Serialize<CardElement>(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void GraphComponents_JsonSerialize_Derived_FromClass()
        {
            AdaptiveCard card = SetupAdaptiveCardWithGraphComponents();
            var expectedJson = File.ReadAllText(@"../../../Json/GraphComponents.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void GraphComponents_JsonSerialize_Derived_FromBaseClass()
        {
            CardElement card = SetupAdaptiveCardWithGraphComponents();
            var expectedJson = File.ReadAllText(@"../../../Json/GraphComponents.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void AdaptiveCardWithComponents_JsonDeserialize()
        {
            var json = File.ReadAllText(@"../../../Json/GraphComponents.json");
            var card = JsonSerializer.Deserialize<AdaptiveCard>(json, JsonOptions);
            var expected = SetupAdaptiveCardWithGraphComponents();

            Assert.NotNull(card);
            Assert.Equal(expected.ToString(), card!.ToString());
            Assert.NotNull(card.Body);

            Assert.Contains(card.Body, e => e is TextBlock);
            Assert.Contains(card.Body, e => e is ComUserMicrosoftGraphComponent);
            Assert.Contains(card.Body, e => e is ComUsersMicrosoftGraphComponent);
            Assert.Contains(card.Body, e => e is ComResourceMicrosoftGraphComponent);
            Assert.Contains(card.Body, e => e is ComFileMicrosoftGraphComponent);
            Assert.Contains(card.Body, e => e is ComEventMicrosoftGraphComponent);
        }

        [Fact]
        public void Components_RoundTripSerialization()
        {
            var originalJson = File.ReadAllText(@"../../../Json/GraphComponents.json");

            var card = JsonSerializer.Deserialize<AdaptiveCard>(originalJson, JsonOptions);

            var serializedJson = JsonSerializer.Serialize<CardElement>(card!, JsonOptions);

            string expectedNormalized = Regex.Replace(originalJson, @"\s+", "");
            string actualNormalized = Regex.Replace(serializedJson, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }
    }
}
