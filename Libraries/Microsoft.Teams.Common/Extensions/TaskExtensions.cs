namespace Microsoft.Teams.Common.Extensions;

public static class TaskExtensions
{
    public static async Task<T> Retry<T>(this Task<T> task, int max = 3, int delay = 200)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {                
            if (max > 0)
            {
                await Task.Delay(delay);
                return await task.Retry(max - 1, delay * 2).ConfigureAwait(false);
            }

            throw new Exception(ex.Message, ex);
        }
    }
}