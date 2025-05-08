namespace Microsoft.Teams.Cards;

public class SignInAction : SubmitAction
{
    public SignInAction(string value)
    {
        Data = new()
        {
            MsTeams = new SigninSubmitActionData(value)
        };
    }
}