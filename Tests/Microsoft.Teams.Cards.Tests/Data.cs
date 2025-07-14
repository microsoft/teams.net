using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards.Tests
{
    public class AdaptiveCardDataTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AdaptiveCard SetupAdaptiveCardWithData()
        {
            var body = new List<CardElement>
            {
                new FactSet()
                    .WithFacts(new List<Fact>
                    {
                        new Fact("Name", "John Doe"),
                        new Fact("Department", "Engineering"),
                        new Fact("Start Date", "2023-01-15"),
                        new Fact("Location", "Seattle, WA"),
                        new Fact("Employee ID", "EMP001")
                    }),

                new Table()
                    .WithColumns(new List<ColumnDefinition>
                    {
                        new ColumnDefinition().WithWidth(new Union<string, float>(1.0f)),
                        new ColumnDefinition().WithWidth(new Union<string, float>(1.0f)),
                        new ColumnDefinition().WithWidth(new Union<string, float>(1.0f))
                    })
                    .WithRows(new List<TableRow>
                    {
                        new TableRow()
                            .WithCells(new List<TableCell>
                            {
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("Product").WithWeight(TextWeight.Bolder)
                                }),
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("Description").WithWeight(TextWeight.Bolder)
                                }),
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("Price").WithWeight(TextWeight.Bolder)
                                })
                            }),
                        new TableRow()
                            .WithCells(new List<TableCell>
                            {
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("Widget A")
                                }),
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("High-quality widget for everyday use")
                                }),
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("$29.99")
                                })
                            }),
                        new TableRow()
                            .WithCells(new List<TableCell>
                            {
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("Widget B")
                                }),
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("Premium widget with advanced features")
                                }),
                                new TableCell().WithItems(new List<CardElement>
                                {
                                    new TextBlock("$49.99")
                                })
                            })
                    }),

                new ColumnSet()
                    .WithColumns(new List<Column>
                    {
                        new Column()
                            .WithWidth(new Union<string, float>("auto"))
                            .WithItems(new List<CardElement>
                            {
                                new TextBlock("Left Column")
                                    .WithWeight(TextWeight.Bolder)
                                    .WithHorizontalAlignment(HorizontalAlignment.Center),
                                new TextBlock("Content in the left column with some sample text.")
                                    .WithWrap(true)
                            }),
                        new Column()
                            .WithWidth(new Union<string, float>("stretch"))
                            .WithItems(new List<CardElement>
                            {
                                new TextBlock("Right Column")
                                    .WithWeight(TextWeight.Bolder)
                                    .WithHorizontalAlignment(HorizontalAlignment.Center),
                                new TextBlock("Content in the right column with different information.")
                                    .WithWrap(true)
                            })
                    }),

                new CodeBlock()
                    .WithCodeSnippet("function echo(a) {\n    return a;\n}")
                    .WithLanguage(CodeLanguage.JavaScript)
            };

            var card = new AdaptiveCard().WithVersion(Version.Version1_5).WithId("dataCard")
                .WithSchema("https://adaptivecards.io/schemas/adaptive-card.json")
                .WithBody(body);
            return card;
        }

        [Fact]
        public void Data_JsonSerialize()
        {
            var card = SetupAdaptiveCardWithData();
            var expectedJson = File.ReadAllText(@"../../../Json/Data.json");

            var json = JsonSerializer.Serialize<CardElement>(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Data_JsonSerialize_Derived_FromClass()
        {
            AdaptiveCard card = SetupAdaptiveCardWithData();
            var expectedJson = File.ReadAllText(@"../../../Json/Data.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Data_JsonSerialize_Derived_FromBaseClass()
        {
            CardElement card = SetupAdaptiveCardWithData();
            var expectedJson = File.ReadAllText(@"../../../Json/Data.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Data_JsonDeserialize()
        {
            var json = File.ReadAllText(@"../../../Json/Data.json");
            var card = JsonSerializer.Deserialize<AdaptiveCard>(json, JsonOptions);
            var expected = SetupAdaptiveCardWithData();

            Assert.NotNull(card);
            Assert.Equal(expected.ToString(), card!.ToString());
            Assert.NotNull(card.Body);

            Assert.Contains(card.Body, e => e is FactSet);
            Assert.Contains(card.Body, e => e is Table);
            Assert.Contains(card.Body, e => e is ColumnSet);
            Assert.Contains(card.Body, e => e is CodeBlock);

            var factSet = card.Body.OfType<FactSet>().FirstOrDefault();
            Assert.NotNull(factSet);
            Assert.NotNull(factSet.Facts);

            var table = card.Body.OfType<Table>().FirstOrDefault();
            Assert.NotNull(table);
            Assert.NotNull(table.Rows);

            var columnSet = card.Body.OfType<ColumnSet>().FirstOrDefault();
            Assert.NotNull(columnSet);
            Assert.NotNull(columnSet.Columns);
        }

        [Fact]
        public void Data_RoundTripSerialization()
        {
            var originalJson = File.ReadAllText(@"../../../Json/Data.json");

            var card = JsonSerializer.Deserialize<AdaptiveCard>(originalJson, JsonOptions);

            var serializedJson = JsonSerializer.Serialize<CardElement>(card!, JsonOptions);

            string expectedNormalized = Regex.Replace(originalJson, @"\s+", "");
            string actualNormalized = Regex.Replace(serializedJson, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }
    }
}
