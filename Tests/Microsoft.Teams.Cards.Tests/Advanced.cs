using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards.Tests
{
    public class AdaptiveCardAdvancedTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AdaptiveCard SetupAdaptiveCardWithAdvanced()
        {
            var body = new List<CardElement>
            {
                new RichTextBlock()
                    .WithInlines(new Union<IList<TextRun>, IList<string>>(new List<TextRun>
                    {
                        new TextRun("This text includes ")
                            .WithWeight(TextWeight.Default),
                        new TextRun("bold")
                            .WithWeight(TextWeight.Bolder),
                        new TextRun(", ")
                            .WithWeight(TextWeight.Default),
                        new TextRun("italic")
                            .WithItalic(true),
                        new TextRun(", and ")
                            .WithWeight(TextWeight.Default),
                        new TextRun("colored")
                            .WithColor(TextColor.Good),
                        new TextRun(" text.")
                            .WithWeight(TextWeight.Default)
                    })),

                new Badge()
                    .WithIcon("star")
                    .WithStyle(BadgeStyle.Good)
                    .WithTooltip("Top Performer"),

                new CompoundButton()
                    .WithTitle("Learn More")
                    .WithDescription("Click to read more about adaptive cards")
                    .WithIcon(new IconInfo()
                        .WithName("info")),

                new Carousel()
                    .WithPages(new List<CarouselPage>
                    {
                        new CarouselPage()
                            .WithItems(new List<CardElement>
                            {
                                new TextBlock("Welcome to page 1")
                                    .WithSize(TextSize.Large)
                                    .WithWeight(TextWeight.Bolder)
                            }),
                        new CarouselPage()
                            .WithItems(new List<CardElement>
                            {
                                new TextBlock("This is page 2")
                                    .WithSize(TextSize.Large)
                                    .WithWeight(TextWeight.Bolder)
                            })
                    }),

                new Rating()
                    .WithValue(4.5f)
            };

            var card = new AdaptiveCard().WithVersion(Version.Version1_5).WithId("advancedCard")
                .WithSchema("https://adaptivecards.io/schemas/adaptive-card.json")
                .WithBody(body);
            return card;
        }

        [Fact]
        public void Advanced_JsonSerialize()
        {
            var card = SetupAdaptiveCardWithAdvanced();
            var expectedJson = File.ReadAllText(@"../../../Json/Advanced.json");

            var json = JsonSerializer.Serialize<CardElement>(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Advanced_JsonSerialize_Derived_FromClass()
        {
            AdaptiveCard card = SetupAdaptiveCardWithAdvanced();
            var expectedJson = File.ReadAllText(@"../../../Json/Advanced.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Advanced_JsonSerialize_Derived_FromBaseClass()
        {
            CardElement card = SetupAdaptiveCardWithAdvanced();
            var expectedJson = File.ReadAllText(@"../../../Json/Advanced.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Advanced_JsonDeserialize()
        {
            var json = File.ReadAllText(@"../../../Json/Advanced.json");
            var card = JsonSerializer.Deserialize<AdaptiveCard>(json, JsonOptions);
            var expected = SetupAdaptiveCardWithAdvanced();

            Assert.NotNull(card);
            Assert.Equal(expected.ToString(), card!.ToString());
            Assert.NotNull(card.Body);

            Assert.Contains(card.Body, e => e is RichTextBlock);
            Assert.Contains(card.Body, e => e is Badge);
            Assert.Contains(card.Body, e => e is CompoundButton);
            Assert.Contains(card.Body, e => e is Carousel);
            Assert.Contains(card.Body, e => e is Rating);

            var richTextBlock = card.Body.OfType<RichTextBlock>().FirstOrDefault();
            Assert.NotNull(richTextBlock);
            Assert.NotNull(richTextBlock.Inlines);

            var carousel = card.Body.OfType<Carousel>().FirstOrDefault();
            Assert.NotNull(carousel);
            Assert.NotNull(carousel.Pages);

            var rating = card.Body.OfType<Rating>().FirstOrDefault();
            Assert.NotNull(rating);
        }

        [Fact]
        public void Advanced_RoundTripSerialization()
        {
            var originalJson = File.ReadAllText(@"../../../Json/Advanced.json");

            var card = JsonSerializer.Deserialize<AdaptiveCard>(originalJson, JsonOptions);

            var serializedJson = JsonSerializer.Serialize<CardElement>(card!, JsonOptions);

            string expectedNormalized = Regex.Replace(originalJson, @"\s+", "");
            string actualNormalized = Regex.Replace(serializedJson, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized); 
        }
    }
}
