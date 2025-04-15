using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class MessageExtensions : StringEnum
    {
        public static readonly MessageExtensions SelectItem = new("composeExtension/selectItem");
        public bool IsSelectItem => SelectItem.Equals(Value);
    }
}

public static partial class MessageExtensions
{
    public class SelectItemActivity() : MessageExtensionActivity(Name.MessageExtensions.SelectItem)
    {
    }
}