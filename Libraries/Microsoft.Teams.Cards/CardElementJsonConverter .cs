using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Cards;

public class CardElementJsonConverter : JsonConverter<CardElement>
{
    public override CardElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var jsonObject = jsonDoc.RootElement;

        if (!jsonObject.TryGetProperty("type", out var typeProp))
            throw new JsonException("Missing 'type' discriminator in CardElement");

        var typeString = typeProp.GetString();

        CardElement? cardElement = typeString switch
        {
            "AdaptiveCard" => JsonSerializer.Deserialize<AdaptiveCard>(jsonObject.GetRawText(), options),
            "Container" => JsonSerializer.Deserialize<Container>(jsonObject.GetRawText(), options), 
            "ActionSet" => JsonSerializer.Deserialize<ActionSet>(jsonObject.GetRawText(), options),
            "ColumnSet" => JsonSerializer.Deserialize<ColumnSet>(jsonObject.GetRawText(), options),
            "Media" => JsonSerializer.Deserialize<Media>(jsonObject.GetRawText(), options),
            "RichTextBlock" => JsonSerializer.Deserialize<RichTextBlock>(jsonObject.GetRawText(), options),
            "Table" => JsonSerializer.Deserialize<Table>(jsonObject.GetRawText(), options),
            "TextBlock" => JsonSerializer.Deserialize<TextBlock>(jsonObject.GetRawText(), options),
            "FactSet" => JsonSerializer.Deserialize<FactSet>(jsonObject.GetRawText(), options),
            "ImageSet" => JsonSerializer.Deserialize<ImageSet>(jsonObject.GetRawText(), options),
            "Image" => JsonSerializer.Deserialize<Image>(jsonObject.GetRawText(), options),
            "Input.Text" => JsonSerializer.Deserialize<TextInput>(jsonObject.GetRawText(), options),
            "Input.Date" => JsonSerializer.Deserialize<DateInput>(jsonObject.GetRawText(), options),
            "Input.Time" => JsonSerializer.Deserialize<TimeInput>(jsonObject.GetRawText(), options),
            "Input.Number" => JsonSerializer.Deserialize<NumberInput>(jsonObject.GetRawText(), options),
            "Input.Toggle" => JsonSerializer.Deserialize<ToggleInput>(jsonObject.GetRawText(), options),
            "Input.ChoiceSet" => JsonSerializer.Deserialize<ChoiceSetInput>(jsonObject.GetRawText(), options),
            "Input.Rating" => JsonSerializer.Deserialize<RatingInput>(jsonObject.GetRawText(), options),
            "Rating" => JsonSerializer.Deserialize<Rating>(jsonObject.GetRawText(), options),
            "CompoundButton" => JsonSerializer.Deserialize<CompoundButton>(jsonObject.GetRawText(), options),
            "Icon" => JsonSerializer.Deserialize<Icon>(jsonObject.GetRawText(), options),
            "Carousel" => JsonSerializer.Deserialize<Carousel>(jsonObject.GetRawText(), options),
            "Badge" => JsonSerializer.Deserialize<Badge>(jsonObject.GetRawText(), options),
            "Chart.Donut" => JsonSerializer.Deserialize<DonutChart>(jsonObject.GetRawText(), options),
            "Chart.Pie" => JsonSerializer.Deserialize<PieChart>(jsonObject.GetRawText(), options),
            "Chart.VerticalBar.Grouped" => JsonSerializer.Deserialize<GroupedVerticalBarChart>(jsonObject.GetRawText(), options),
            "Chart.VerticalBar" => JsonSerializer.Deserialize<VerticalBarChart>(jsonObject.GetRawText(), options),
            "Chart.HorizontalBar" => JsonSerializer.Deserialize<HorizontalBarChart>(jsonObject.GetRawText(), options),
            "Chart.HorizontalBar.Stacked" => JsonSerializer.Deserialize<StackedHorizontalBarChart>(jsonObject.GetRawText(), options),
            "Chart.Line" => JsonSerializer.Deserialize<LineChart>(jsonObject.GetRawText(), options),
            "Chart.Gauge" => JsonSerializer.Deserialize<GaugeChart>(jsonObject.GetRawText(), options),
            "CodeBlock" => JsonSerializer.Deserialize<CodeBlock>(jsonObject.GetRawText(), options),
            "Component.User" => JsonSerializer.Deserialize<ComUserMicrosoftGraphComponent>(jsonObject.GetRawText(), options),
            "Component.Users" => JsonSerializer.Deserialize<ComUsersMicrosoftGraphComponent>(jsonObject.GetRawText(), options),
            "Component.Resource" => JsonSerializer.Deserialize<ComResourceMicrosoftGraphComponent>(jsonObject.GetRawText(), options),
            "Component.File" => JsonSerializer.Deserialize<ComFileMicrosoftGraphComponent>(jsonObject.GetRawText(), options),
            "Component.Event" => JsonSerializer.Deserialize<ComEventMicrosoftGraphComponent>(jsonObject.GetRawText(), options),
            "CarouselPage" => JsonSerializer.Deserialize<CarouselPage>(jsonObject.GetRawText(), options),
            "TableRow" => JsonSerializer.Deserialize<TableRow>(jsonObject.GetRawText(), options),
            "TableCell" => JsonSerializer.Deserialize<TableCell>(jsonObject.GetRawText(), options),
            "TextRun" => JsonSerializer.Deserialize<TextRun>(jsonObject.GetRawText(), options),
            "Column" => JsonSerializer.Deserialize<Column>(jsonObject.GetRawText(), options),
            _ => throw new JsonException($"Unknown card element type: {typeString}")
        };

        return cardElement;
    }

    public override void Write(Utf8JsonWriter writer, CardElement value, JsonSerializerOptions options)
    {
        var type = value.GetType();
        JsonSerializer.Serialize(writer, value, type, options);
    }

}
