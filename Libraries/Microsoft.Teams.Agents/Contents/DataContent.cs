using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Agents;

public static partial class Contents
{
    public static DataContent Data(object data) => new(data);
}

public partial class ContentType
{
    public static readonly ContentType Data = new("data");
    public bool IsData => Data.Equals(Value);
}

[TrueTypeJson<IDataContent>]
public interface IDataContent : IContent
{
    public object Data { get; }
}

public class DataContent(object data) : IDataContent
{
    public ContentType Type => ContentType.Data;
    public object Data { get; set; } = data;
}