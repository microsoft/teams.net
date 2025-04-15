namespace Microsoft.Teams.AI.Prompts;

public partial class ChatPrompt<TOptions>
{
    public IChatPrompt<TOptions> OnError(Action<Exception> onError)
    {
        ErrorEvent += (_, ex) => onError(ex);
        return this;
    }

    public IChatPrompt<TOptions> OnError(Func<Exception, Task> onError)
    {
        ErrorEvent += (_, ex) => onError(ex).GetAwaiter().GetResult();
        return this;
    }
}