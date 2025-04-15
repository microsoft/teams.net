using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Web;

namespace Microsoft.Teams.Common.Http;

public static class QueryString
{
    public static string Serialize(object value)
    {
        var properties = value.GetType().GetProperties();
        var parts = new List<string>();

        foreach (var property in properties)
        {
            var builder = new StringBuilder();
            var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var name = jsonAttribute?.Name ?? property.Name;

            builder.Append(HttpUtility.UrlEncode(name));
            builder.Append('=');
            builder.Append(HttpUtility.UrlEncode(property.GetValue(value, null)?.ToString()));
            parts.Add(builder.ToString());
        }

        return string.Join('&', parts);
    }
}