using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Search;
using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public static readonly Name Search = new("application/search");
    public bool IsSearch => Search.Equals(Value);
}

public class SearchActivity() : InvokeActivity(Name.Search)
{
    /// <summary>
    /// A value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(32)]
    public new required SearchValue Value
    {
        get => (SearchValue)base.Value!;
        set => base.Value = value;
    }
}