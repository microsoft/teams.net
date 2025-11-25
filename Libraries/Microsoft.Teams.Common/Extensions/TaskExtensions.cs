// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Extensions;

public static class TaskExtensions
{
    public static async Task<T> Retry<T>(Func<Task<T>> taskFactory, int max = 3, int delay = 500)
    {
        try
        {
            return await taskFactory().ConfigureAwait(false);
        }
        // don't retry cancelled
        catch (OperationCanceledException) when (max > 0)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (max > 0)
            {
                await Task.Delay(delay);
                return await Retry(taskFactory, max - 1, delay * 2).ConfigureAwait(false);
            }
            throw new Exception(ex.Message, ex);
        }
    }
}