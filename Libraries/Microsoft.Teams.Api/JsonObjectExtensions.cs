using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

public static partial class JsonObjectExtensions
{
    public static IDictionary<string, object?> FromJsonObject<T>(this T value, JsonObject json, JsonSerializerOptions? options = null) where T : notnull
    {
        var properties = value.GetType().GetProperties();
        var other = new Dictionary<string, object?>();

        foreach (var pair in json)
        {
            var property = properties.FirstOrDefault(f =>
                pair.Key == (f.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? f.Name)
            );

            if (property is null)
            {
                other[pair.Key] = pair.Value.Deserialize<object>(options);
                continue;
            }

            if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
            {
                continue;
            }

            property.SetValue(value, pair.Value.Deserialize(property.PropertyType, options));
        }

        return other;
    }

    public static JsonObject ToJsonObject(this object value, JsonSerializerOptions? options = null)
    {
        var json = new JsonObject();
        var properties = value
            .GetType()
            .GetProperties()
            .OrderBy(p => p.GetCustomAttribute<JsonPropertyOrderAttribute>()?.Order ?? p.MetadataToken);

        foreach (var property in properties)
        {
            var propertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
            var propertyValue = property.GetValue(value);

            if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;
            if (propertyValue is null) continue;
            if (property.GetCustomAttribute<JsonExtensionDataAttribute>() is not null)
            {
                var jsonObject = JsonSerializer.SerializeToNode(propertyValue, propertyValue.GetType(), options)?.AsObject();

                if (jsonObject is null) continue;

                foreach (var item in jsonObject)
                {
                    json.Add(item.Key, item.Value?.DeepClone());
                }

                continue;
            }

            if (!json.ContainsKey(propertyName))
            {
                json.Add(propertyName, JsonSerializer.SerializeToNode(propertyValue, options));
            }
        }

        return json;
    }
}