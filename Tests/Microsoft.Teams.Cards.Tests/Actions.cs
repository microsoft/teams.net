using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Teams.Cards.Tests
{
    public class Actions
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AdaptiveCard SetupAdaptiveCardWithActions()
        {
            var body = new List<CardElement>
            {
                new TextInput()
                    .WithId("fullName")
                    .WithLabel("Full Name")
                    .WithPlaceholder("Enter your full name")
                    .WithIsRequired(true)
                    .WithErrorMessage("Please enter your name"),

                new TextInput()
                    .WithId("email")
                    .WithLabel("Email Address")
                    .WithPlaceholder("Enter your email")
                    .WithStyle(InputTextStyle.Email)
                    .WithIsRequired(true)
                    .WithErrorMessage("Please enter your email"),

                new NumberInput()
                    .WithId("age")
                    .WithLabel("Age")
                    .WithPlaceholder("Enter your age")
                    .WithMin(18)
                    .WithMax(120)
                    .WithValue(25),

                new ChoiceSetInput()
                    .WithId("department")
                    .WithLabel("Department")
                    .WithStyle(StyleEnum.Expanded)
                    .WithChoices(new List<Choice>
                    {
                        new Choice().WithTitle("Engineering").WithValue("eng"),
                        new Choice().WithTitle("Marketing").WithValue("marketing"),
                        new Choice().WithTitle("Sales").WithValue("sales"),
                        new Choice().WithTitle("Support").WithValue("support"),
                    }),

                new ToggleInput("Subscribe to newsletter")
                    .WithId("newsletter")
                    .WithValueOn("yes")
                    .WithValueOff("no")
                    .WithValue("true"),

                new RatingInput()
                    .WithId("satisfaction")
                    .WithLabel("Satisfaction Rating"),

                new DateInput()
                    .WithId("eventDate")
                    .WithLabel("Event Date")
                    .WithPlaceholder("Select a date")
                    .WithIsRequired(true)
                    .WithErrorMessage("Please select a date"),

                new TimeInput()
                    .WithId("eventTime")
                    .WithLabel("Event Time")
                    .WithPlaceholder("Select a time")
                    .WithIsRequired(true)
                    .WithErrorMessage("Please select a time"),

                new ActionSet(
                new SubmitAction()
                    .WithId("submit-all")
                    .WithTitle("Submit Form")
                    .WithStyle(ActionStyle.Positive)
                    .WithData(new SubmitActionData
                    {
                        NonSchemaProperties = new Dictionary<string, object?> { ["action"] = "submit" }
                    }),

                new OpenUrlAction("https://example.com")
                    .WithId("visit-website")
                    .WithTitle("Visit Website")
                    .WithIconUrl("globe,regular"),

                new ExecuteAction()
                    .WithId("validate")
                    .WithTitle("Validate")
                    .WithVerb("validate")
                    .WithData(new SubmitActionData
                    {
                        NonSchemaProperties = new Dictionary<string, object?> { ["action"] = "validate" }
                    }),

                new ToggleVisibilityAction()
                    .WithId("toggle-advanced")
                    .WithTitle("Toggle Advanced")
                    .WithTargetElements(new List<TargetElement>
                    {
                        new TargetElement().WithElementId("advanced-section").WithIsVisible(true)
                    })
                )
            };

            var card = new AdaptiveCard().WithVersion(Cards.Version.Version1_5).WithId("actions-card")
            .WithSchema("https://adaptivecards.io/schemas/adaptive-card.json")
            .WithBody(body);
            return card;
        }


        [Fact]
        public void Actions_JsonSerialize()
        {
            var card = SetupAdaptiveCardWithActions();
            var expectedJson = File.ReadAllText(@"../../../Json/Actions.json");

            var json = JsonSerializer.Serialize<CardElement>(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Actions_JsonSerialize_Derived_FromClass()
        {
            AdaptiveCard card = SetupAdaptiveCardWithActions();
            var expectedJson = File.ReadAllText(@"../../../Json/Actions.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Actions_JsonSerialize_Derived_FromBaseClass()
        {
            CardElement card = SetupAdaptiveCardWithActions();
            var expectedJson = File.ReadAllText(@"../../../Json/Actions.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Actions_JsonDeserialize()
        {
            var json = File.ReadAllText(@"../../../Json/Actions.json");
            var card = JsonSerializer.Deserialize<AdaptiveCard>(json, JsonOptions);
            var expected = SetupAdaptiveCardWithActions();

            Assert.NotNull(card);
            Assert.Equal(expected.ToString(), card!.ToString());
            Assert.NotNull(card.Body);

            Assert.Contains(card.Body, e => e is TextInput);
            Assert.Contains(card.Body, e => e is NumberInput );
            Assert.Contains(card.Body, e => e is ChoiceSetInput);
            Assert.Contains(card.Body, e => e is ToggleInput);
            Assert.Contains(card.Body, e => e is RatingInput);
            Assert.Contains(card.Body, e => e is DateInput);
            Assert.Contains(card.Body, e => e is TimeInput);

            var actionSet = card.Body.OfType<ActionSet>().FirstOrDefault();
            Assert.NotNull(actionSet);
            Assert.NotNull(actionSet.Actions);
            Assert.Contains(actionSet.Actions, a => a is SubmitAction);
            Assert.Contains(actionSet.Actions, a => a is OpenUrlAction);
            Assert.Contains(actionSet.Actions, a => a is ExecuteAction);
            Assert.Contains(actionSet.Actions, a => a is ToggleVisibilityAction);
        }

        [Fact]
        public void Actions_RoundTripSerialization()
        {
            var originalJson = File.ReadAllText(@"../../../Json/Actions.json");

            var card = JsonSerializer.Deserialize<AdaptiveCard>(originalJson, JsonOptions);

            var serializedJson = JsonSerializer.Serialize<CardElement>(card!, JsonOptions);

            string expectedNormalized = Regex.Replace(originalJson, @"\s+", "");
            string actualNormalized = Regex.Replace(serializedJson, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }
    }
}
