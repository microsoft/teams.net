// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            if (property.PropertyType == typeof(IList<string>))
            {
                SerializeIListString(value, property, parts);
                continue;
            }
            var builder = new StringBuilder();
            var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var name = jsonAttribute?.Name ?? property.Name;

            builder.Append(HttpUtility.UrlEncode(name));
            builder.Append('=');
            builder.Append(HttpUtility.UrlEncode(property.GetValue(value, null)?.ToString()));
            parts.Add(builder.ToString());
        }

        return string.Join("&", parts);
    }

    private static void SerializeIListString(object value, PropertyInfo property, List<string> parts)
    {
        var jsonAttributeList = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        var nameList = jsonAttributeList?.Name ?? property.Name;
        var listObject = property.GetValue(value, null) as IList<string>;
        if (listObject != null)
        {
            for (int i = 0; i < listObject.Count; i++)
            {
                if (listObject[i] != null)
                {
                    var builder = new StringBuilder();
                    builder.Append(HttpUtility.UrlEncode(nameList));
                    builder.Append(HttpUtility.UrlEncode($"[{i}]"));
                    builder.Append('=');
                    builder.Append(HttpUtility.UrlEncode(listObject[i]));
                    parts.Add(builder.ToString());
                }
            }
        }
    }
}