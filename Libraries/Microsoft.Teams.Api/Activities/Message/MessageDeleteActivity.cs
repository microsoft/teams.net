using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType MessageDelete = new("messageDelete");
    public bool IsMessageDelete => MessageDelete.Equals(Value);
}

public class MessageDeleteActivity() : Activity(ActivityType.MessageDelete)
{
}