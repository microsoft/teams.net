using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Teams.Cards.Tests
{
    public class AdaptiveCardMediaTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AdaptiveCard SetupAdaptiveCardWithMedia()
        {
            var body = new List<CardElement>
            {
                new Image("https://picsum.photos/400/200")
                    .WithAltText("Sample landscape image")
                    .WithStyle(ImageStyle.RoundedCorners)
                    .WithSize(Size.Large)
                    .WithHorizontalAlignment(HorizontalAlignment.Center),

                new Media()
                    .WithPoster("https://adaptivecards.io/content/poster-video.png")
                    .WithSources(new List<MediaSource>
                    {
                        new MediaSource()
                            .WithMimeType("video/mp4")
                            .WithUrl("https://adaptivecardsblob.blob.core.windows.net/assets/AdaptiveCardsOverviewVideo.mp4")
                    }),

                new ImageSet()
                    .WithImages(new List<Image>
                    {
                        new Image("https://picsum.photos/100/100?random=1")
                            .WithAltText("Random image 1")
                            .WithSize(Size.Medium),
                        new Image("https://picsum.photos/100/100?random=2")
                            .WithAltText("Random image 2")
                            .WithSize(Size.Medium),
                        new Image("https://picsum.photos/100/100?random=3")
                            .WithAltText("Random image 3")
                            .WithSize(Size.Medium)
                    })
            };

            var card = new AdaptiveCard().WithVersion(Version.Version1_5).WithId("mediaCard")
                .WithSchema("http://adaptivecards.io/schemas/adaptive-card.json")
                .WithBody(body);
            return card;
        }

        [Fact]
        public void AdaptiveCardWithMedia_JsonSerialize()
        {
            var card = SetupAdaptiveCardWithMedia();
            var expectedJson = File.ReadAllText(@"../../../Json/Media.json");

            var json = JsonSerializer.Serialize<CardElement>(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void AdaptiveCardWithMedia_Serialize_Derived_FromClass()
        {
            AdaptiveCard card = SetupAdaptiveCardWithMedia();
            var expectedJson = File.ReadAllText(@"../../../Json/Media.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void AdaptiveCardWithMedia_Serialize_Derived_FromBaseClass()
        {
            CardElement card = SetupAdaptiveCardWithMedia();
            var expectedJson = File.ReadAllText(@"../../../Json/Media.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void AdaptiveCardWithMedia_JsonDeserialize()
        {
            var json = File.ReadAllText(@"../../../Json/Media.json");
            var card = JsonSerializer.Deserialize<AdaptiveCard>(json, JsonOptions);
            var expected = SetupAdaptiveCardWithMedia();

            Assert.NotNull(card);
            Assert.Equal(expected.ToString(), card!.ToString());
            Assert.NotNull(card.Body);

            Assert.Contains(card.Body, e => e is Image);
            Assert.Contains(card.Body, e => e is Media);
            Assert.Contains(card.Body, e => e is ImageSet);

            var imageSet = card.Body.OfType<ImageSet>().FirstOrDefault();
            Assert.NotNull(imageSet);
            Assert.NotNull(imageSet.Images);
        }

        [Fact]
        public void AdaptiveCardWithMedia_RoundTripSerialization()
        {
            var originalJson = File.ReadAllText(@"../../../Json/Media.json");

            var card = JsonSerializer.Deserialize<AdaptiveCard>(originalJson, JsonOptions);

            var serializedJson = JsonSerializer.Serialize<CardElement>(card!, JsonOptions);

            string expectedNormalized = Regex.Replace(originalJson, @"\s+", "");
            string actualNormalized = Regex.Replace(serializedJson, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }
    }
}
