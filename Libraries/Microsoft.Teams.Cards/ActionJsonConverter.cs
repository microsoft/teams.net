using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Cards;

public class ActionJsonConverter : JsonConverter<Microsoft.Teams.Cards.Action>
{
    public override Microsoft.Teams.Cards.Action? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var jsonObject = jsonDoc.RootElement;

        if (!jsonObject.TryGetProperty("type", out var typeProp))
            throw new JsonException("Missing 'type' discriminator in Action");

        var typeString = typeProp.GetString();

        return typeString switch
        {
            "Action.OpenUrl" => JsonSerializer.Deserialize<OpenUrlAction>(jsonObject.GetRawText(), options),
            "Action.Submit" => JsonSerializer.Deserialize<SubmitAction>(jsonObject.GetRawText(), options),
            "Action.ShowCard" => JsonSerializer.Deserialize<ShowCardAction>(jsonObject.GetRawText(), options),
            "Action.ToggleVisibility" => JsonSerializer.Deserialize<ToggleVisibilityAction>(jsonObject.GetRawText(), options),
            "Action.Execute" => JsonSerializer.Deserialize<ExecuteAction>(jsonObject.GetRawText(), options),
            _ => throw new JsonException($"Unknown action type: {typeString}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Microsoft.Teams.Cards.Action value, JsonSerializerOptions options)
    {
        var actualType = value.GetType();
        JsonSerializer.Serialize(writer, value, actualType, options);
    }
}
