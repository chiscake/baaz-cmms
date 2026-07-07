using System;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;

namespace BAAZ.CMMS.App.Helpers;

internal static class UiDispatchHelper
{
    public static Task RunAsync(DispatcherQueue dispatcherQueue, Action action)
    {
        var tcs = new TaskCompletionSource();
        if (!dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            tcs.SetException(new InvalidOperationException("Не удалось поставить задачу в очередь UI-потока."));
        }

        return tcs.Task;
    }
}
