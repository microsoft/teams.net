using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Events;

public partial class Name : StringEnum
{
    public static readonly Name ReadReceipt = new("application/vnd.microsoft.readReceipt");
    public bool IsReadReceipt => ReadReceipt.Equals(Value);
}

public class ReadReceiptActivity() : EventActivity(Name.ReadReceipt)
{

}