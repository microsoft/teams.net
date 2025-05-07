namespace Microsoft.Teams.Cards;

public class MessageBackAction : SubmitAction
{
    public MessageBackAction(string text, string value)
    {
        Data = new()
        {
            MsTeams = new MessageBackSubmitActionData()
            {
                Text = text,
                Value = value
            }
        };
    }

    public MessageBackAction(string text, string displayText, string value)
    {
        Data = new()
        {
            MsTeams = new MessageBackSubmitActionData()
            {
                Text = text,
                DisplayText = displayText,
                Value = value
            }
        };
    }
}