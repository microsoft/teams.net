using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class Tabs : StringEnum
    {
        public static readonly Tabs Submit = new("tab/submit");
        public bool IsSubmit => Submit.Equals(Value);
    }
}

public static partial class Tabs
{
    public class SubmitActivity() : TabActivity(Name.Tabs.Submit)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required Api.Tabs.Request Value { get; set; }
    }
}