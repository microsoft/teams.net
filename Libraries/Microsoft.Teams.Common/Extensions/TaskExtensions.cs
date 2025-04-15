namespace Microsoft.Teams.Common.Extensions;

public static class TaskExtensions
{
    public static async Task<T> Retry<T>(this Task<T> task, int max = 10, int delay = 200)
    {
        int attempts = 0;
        int pow = 1;
        List<Exception> exceptions = [];

        Task Delay()
        {
            attempts++;

            if (attempts < 31)
            {
                pow <<= 1;
            }

            var ms = Math.Min(delay * (pow - 1) / 2, delay);
            return Task.Delay(ms);
        }

        for (var i = 0; i < max; i++)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                await Delay().ConfigureAwait(false);
            }
        }

        throw new AggregateException(exceptions);
    }
}