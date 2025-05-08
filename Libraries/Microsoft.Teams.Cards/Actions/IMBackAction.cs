namespace Microsoft.Teams.Cards;

public class IMBackAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    /// 
    public IMBackAction(string value)
    {
        Data = new()
        {
            MsTeams = new ImBackSubmitActionData(value)
        };
    }
}