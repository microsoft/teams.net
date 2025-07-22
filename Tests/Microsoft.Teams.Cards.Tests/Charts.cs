using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards.Tests
{
    public class Charts
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true, 
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public AdaptiveCard SetupAdaptiveCardWithCharts()
        {
            var container = new Container()
                .WithItems(new List<CardElement>
                {
                    new TextBlock("Charts Section")
                        .WithWeight(TextWeight.Bolder)
                        .WithColor(TextColor.Attention)
                        .WithSize(TextSize.Medium),

                    new DonutChart()
                        .WithTitle("Sales Distribution")
                        .WithData(new List<DonutChartData>
                        {
                            new DonutChartData().WithLegend("Q1").WithValue(25).WithColor(ChartColor.CategoricalBlue),
                            new DonutChartData().WithLegend("Q2").WithValue(30).WithColor(ChartColor.CategoricalGreen),
                            new DonutChartData().WithLegend("Q3").WithValue(25).WithColor(ChartColor.CategoricalPurple),
                            new DonutChartData().WithLegend("Q4").WithValue(20).WithColor(ChartColor.CategoricalTeal),
                        }),

                    new GaugeChart()
                        .WithTitle("Performance Score")
                        .WithValue(85)
                        .WithMin(0)
                        .WithMax(100)
                        .WithValueFormat(GaugeChartValueFormat.Percentage)
                        .WithSegments(new List<GaugeChartLegend>
                        {
                            new GaugeChartLegend().WithSize(60).WithLegend("Poor").WithColor(ChartColor.Attention),
                            new GaugeChartLegend().WithSize(30).WithLegend("Good").WithColor(ChartColor.Warning),
                            new GaugeChartLegend().WithSize(10).WithLegend("Excellent").WithColor(ChartColor.Good),
                        }),

                    new PieChart()
                        .WithTitle("Market Share")
                        .WithData(new List<DonutChartData>
                        {
                            new DonutChartData().WithLegend("Product A").WithValue(40).WithColor(ChartColor.Good),
                            new DonutChartData().WithLegend("Product B").WithValue(30).WithColor(ChartColor.Attention),
                            new DonutChartData().WithLegend("Product C").WithValue(30).WithColor(ChartColor.Warning),
                        }),

                    new GroupedVerticalBarChart()
                        .WithTitle("Departmental Revenue")
                        .WithData(new List<GroupedVerticalBarChartData>
                        {
                            new GroupedVerticalBarChartData()
                                .WithLegend("Engineering")
                                .WithValues(new List<BarChartDataValue>
                                {
                                    new BarChartDataValue().WithX("Q1").WithY(120),
                                    new BarChartDataValue().WithX("Q2").WithY(135),
                                }),
                            new GroupedVerticalBarChartData()
                                .WithLegend("Marketing")
                                .WithValues(new List<BarChartDataValue>
                                {
                                    new BarChartDataValue().WithX("Q1").WithY(110),
                                    new BarChartDataValue().WithX("Q2").WithY(140),
                                })
                        }),

                    new StackedHorizontalBarChart()
                        .WithTitle("Customer Segments")
                        .WithData(new List<StackedHorizontalBarChartData>
                        {
                            new StackedHorizontalBarChartData()
                                .WithTitle("SMBs")
                                .WithData(new List<StackedHorizontalBarChartDataPoint>
                                {
                                    new StackedHorizontalBarChartDataPoint().WithLegend("SMBs").WithValue(60).WithColor(ChartColor.Good),
                                }),
                            new StackedHorizontalBarChartData()
                                .WithTitle("Enterprises")
                                .WithData(new List<StackedHorizontalBarChartDataPoint>
                                {
                                    new StackedHorizontalBarChartDataPoint().WithLegend("Enterprises").WithValue(40).WithColor(ChartColor.Warning),
                                })
                        }),

                    new LineChart()
                        .WithTitle("Monthly Sales Trend")
                        .WithData(new List<LineChartData>
                        {
                            new LineChartData()
                                .WithLegend("2023")
                                .WithValues(new List<LineChartValue>
                                {
                                    new LineChartValue().WithX(new Union<float, string>("Jan")).WithY(100),
                                    new LineChartValue().WithX(new Union<float, string>("Feb")).WithY(120),
                                    new LineChartValue().WithX(new Union<float, string>("Mar")).WithY(110),
                                    new LineChartValue().WithX(new Union<float, string>("Apr")).WithY(130)
                                }),
                            new LineChartData()
                                .WithLegend("2024")
                                .WithValues(new List<LineChartValue>
                                {
                                    new LineChartValue().WithX(new Union<float, string>("Jan")).WithY(110),
                                    new LineChartValue().WithX(new Union<float, string>("Feb")).WithY(125),
                                    new LineChartValue().WithX(new Union<float, string>("Mar")).WithY(115),
                                    new LineChartValue().WithX(new Union<float, string>("Apr")).WithY(140)
                                })
                        }),

                    new VerticalBarChart()
                        .WithTitle("Product Sales")
                        .WithData(new List<VerticalBarChartDataValue>
                        {
                            new VerticalBarChartDataValue().WithX(new Union<string, float>("Product A")).WithY(85).WithColor(ChartColor.CategoricalBlue),
                            new VerticalBarChartDataValue().WithX(new Union<string, float>("Product B")).WithY(92).WithColor(ChartColor.CategoricalGreen),
                            new VerticalBarChartDataValue().WithX(new Union<string, float>("Product C")).WithY(78).WithColor(ChartColor.CategoricalPurple)
                        }),
                    
                    new HorizontalBarChart()
                        .WithTitle("Customer Satisfaction")
                        .WithData(new List<HorizontalBarChartDataValue>
                        {
                            new HorizontalBarChartDataValue().WithX("Service").WithY(88),
                            new HorizontalBarChartDataValue().WithX("Quality").WithY(95),
                            new HorizontalBarChartDataValue().WithX("Value").WithY(82)
                        })
                });

            var body = new List<CardElement> { container };
        
            var card = new AdaptiveCard().WithVersion(Version.Version1_5).WithId("chartsCard")
                .WithSchema("https://adaptivecards.io/schemas/adaptive-card.json")
                .WithBody(body);
            return card;
        }

        [Fact]
        public void Charts_JsonSerialize()
        {
            var card = SetupAdaptiveCardWithCharts();
            var expectedJson = File.ReadAllText(@"../../../Json/Charts.json");

            var json = JsonSerializer.Serialize<CardElement>(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Charts_JsonSerialize_Derived_FromClass()
        {
            AdaptiveCard card = SetupAdaptiveCardWithCharts();
            var expectedJson = File.ReadAllText(@"../../../Json/Charts.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Charts_JsonSerialize_Derived_FromBaseClass()
        {
            CardElement card = SetupAdaptiveCardWithCharts();
            var expectedJson = File.ReadAllText(@"../../../Json/Charts.json");

            var json = JsonSerializer.Serialize(card, JsonOptions);

            string expectedNormalized = Regex.Replace(expectedJson, @"\s+", "");
            string actualNormalized = Regex.Replace(json, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void Charts_JsonDeserialize()
        {
            var json = File.ReadAllText(@"../../../Json/Charts.json");
            var card = JsonSerializer.Deserialize<AdaptiveCard>(json, JsonOptions);
            var expected = SetupAdaptiveCardWithCharts();

            Assert.NotNull(card);
            Assert.Equal(expected.ToString(), card!.ToString());
            Assert.NotNull(card.Body);

            var container = card.Body.OfType<Container>().FirstOrDefault();
            Assert.NotNull(container);
            Assert.NotNull(container.Items);

            Assert.Contains(container.Items, e => e is DonutChart);
            Assert.Contains(container.Items, e => e is GaugeChart);
            Assert.Contains(container.Items, e => e is PieChart);
            Assert.Contains(container.Items, e => e is GroupedVerticalBarChart);
            Assert.Contains(container.Items, e => e is StackedHorizontalBarChart);
            Assert.Contains(container.Items, e => e is LineChart);
            Assert.Contains(container.Items, e => e is VerticalBarChart);
            Assert.Contains(container.Items, e => e is HorizontalBarChart);
        }

        [Fact]
        public void Charts_RoundTripSerialization()
        {
            var originalJson = File.ReadAllText(@"../../../Json/Charts.json");

            var card = JsonSerializer.Deserialize<AdaptiveCard>(originalJson, JsonOptions);

            var serializedJson = JsonSerializer.Serialize<CardElement>(card!, JsonOptions);

            string expectedNormalized = Regex.Replace(originalJson, @"\s+", "");
            string actualNormalized = Regex.Replace(serializedJson, @"\s+", "");
            Assert.Equal(expectedNormalized, actualNormalized);
        }
    }
}
