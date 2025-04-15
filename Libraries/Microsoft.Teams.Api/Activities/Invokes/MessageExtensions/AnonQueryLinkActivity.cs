using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class MessageExtensions : StringEnum
    {
        public static readonly MessageExtensions AnonQueryLink = new("composeExtension/anonymousQueryLink");
        public bool IsAnonQueryLink => AnonQueryLink.Equals(Value);
    }
}

public static partial class MessageExtensions
{
    public class AnonQueryLinkActivity() : MessageExtensionActivity(Name.MessageExtensions.AnonQueryLink)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required AppBasedQueryLink Value { get; set; }
    }
}