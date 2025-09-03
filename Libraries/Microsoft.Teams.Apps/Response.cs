// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Represents a response returned by a bot when it receives an activity.
/// </summary>
public class Response<T> : Response where T : notnull
{
    [JsonPropertyName("body")]
    [JsonPropertyOrder(1)]
    public new T Body { get; set; }

    public Response(T body) : base(HttpStatusCode.OK)
    {
        Body = body;
    }

    public Response(HttpStatusCode status, T body) : base(status)
    {
        Body = body;
    }
}

/// <summary>
/// Represents a response returned by a bot when it receives an activity.
/// </summary>
public class Response
{
    /// <summary>
    /// Response metadata containing information
    /// about the handling of the activity
    /// </summary>
    public MetaData Meta { get; set; }

    /// <summary>
    /// The HTTP status code of the response.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonPropertyOrder(0)]
    public HttpStatusCode Status { get; set; }

    /// <summary>
    /// Optional. The body of the response.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(1)]
    public object? Body { get; set; }

    public Response(object? body)
    {
        Meta = [];
        Status = HttpStatusCode.OK;
        Body = body;
    }

    public Response(HttpStatusCode status = HttpStatusCode.OK, object? body = null)
    {
        Meta = [];
        Status = status;
        Body = body;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Response metadata containing information
    /// about the handling of the activity
    /// </summary>
    public class MetaData : IDictionary<string, object?>
    {
        /// <summary>
        /// The number of activity routes that
        /// were called while processing the activity
        /// </summary>
        [JsonIgnore]
        public int Routes
        {
            get => GetOrDefault<int>("routes");
            set => Data["routes"] = value;
        }

        /// <summary>
        /// The elapse time in milliseconds
        /// </summary>
        [JsonIgnore]
        public int Elapse
        {
            get => GetOrDefault<int>("elapse");
            set => Data["elapse"] = value;
        }

        [JsonIgnore]
        public ICollection<string> Keys => Data.Keys;

        [JsonIgnore]
        public ICollection<object?> Values => Data.Values;

        [JsonIgnore]
        public int Count => Data.Count;

        [JsonIgnore]
        public bool IsReadOnly => Data.IsReadOnly;

        [JsonIgnore]
        public object? this[string key]
        {
            get => Data[key];
            set => Data[key] = value;
        }

        /// <summary>
        /// Custom metadata
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object?> Data = new Dictionary<string, object?>();

        public object? GetOrDefault(string key) => Data.ContainsKey(key) ? Data[key] : null;
        public T? GetOrDefault<T>(string key) => Data.ContainsKey(key) ? (T?)Data[key] : default;
        public void Add(string key, object? value) => Data.Add(key, value);
        public bool ContainsKey(string key) => Data.ContainsKey(key);
        public bool Remove(string key) => Data.Remove(key);
        public bool TryGetValue(string key, out object? value) => Data.TryGetValue(key, out value);
        public void Add(KeyValuePair<string, object?> item) => Data.Add(item);
        public void Clear() => Data.Clear();
        public bool Contains(KeyValuePair<string, object?> item) => Data.Contains(item);
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => Data.CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<string, object?> item) => Data.Remove(item);
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}