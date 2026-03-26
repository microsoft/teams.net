// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Extensions;

public static class ActionExtensions
{
    public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return arg =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource?.Dispose();
            cancelTokenSource = new CancellationTokenSource();

            _ = Task.Delay(milliseconds, cancelTokenSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        func(arg);
                    }
                }, TaskScheduler.Default);
        };
    }

    public static Action Debounce(this Func<Task> func, int milliseconds = 300)
    {
        CancellationTokenSource? cancelTokenSource = null;

        return () =>
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource?.Dispose();
            cancelTokenSource = new CancellationTokenSource();

            _ = DebounceCore(func, milliseconds, cancelTokenSource.Token);
        };

        static async Task DebounceCore(Func<Task> func, int milliseconds, CancellationToken token)
        {
            try
            {
                await Task.Delay(milliseconds, token).ConfigureAwait(false);
                await func().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Debounce was cancelled by a newer invocation
            }
            catch (Exception)
            {
                // Observe exception to prevent UnobservedTaskException.
                // Callers use fire-and-forget; there is no upstream to propagate to.
            }
        }
    }
}
