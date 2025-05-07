namespace Microsoft.Teams.Cards;

public class TaskFetchAction : SubmitAction
{
    public TaskFetchAction(object value)
    {
        Data = new()
        {
            MsTeams = new TaskFetchSubmitActionData(value)
        };
    }
}