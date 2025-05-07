namespace Microsoft.Teams.Cards;

public class InvokeAction : SubmitAction
{
    public InvokeAction(object value)
    {
        Data = new()
        {
            MsTeams = new InvokeSubmitActionData(value)
        };
    }
}