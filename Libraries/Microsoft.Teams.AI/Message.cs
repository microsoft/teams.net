using System.Text.Json.Serialization;

using Microsoft.Teams.Common;
using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.AI;

/// <summary>
/// some message sent to or from the LLM
/// via a Model
/// </summary>
[JsonConverter(typeof(TrueTypeJsonConverter<IMessage>))]
public interface IMessage
{
    /// <summary>
    /// the role of the message, ie
    /// who sent the message
    /// </summary>
    public Role Role { get; }
}

[JsonConverter(typeof(JsonConverter<Role>))]
public class Role(string value) : StringEnum(value)
{
    public static readonly Role User = new("user");
    public bool IsUser => User.Equals(Value);

    public static readonly Role Model = new("model");
    public bool IsModel => Model.Equals(Value);

    public static readonly Role Developer = new("developer");
    public bool IsDeveloper => Developer.Equals(Value);

    public static readonly Role Function = new("function");
    public bool IsFunction => Function.Equals(Value);
}