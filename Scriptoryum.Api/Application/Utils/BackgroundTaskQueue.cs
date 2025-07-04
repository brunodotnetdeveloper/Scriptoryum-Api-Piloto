using System.Collections.Concurrent;
using System.Threading;

namespace Scriptoryum.Api.Application.Utils;

public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
{
    private readonly ConcurrentQueue<T> _workItems = new();
    private readonly SemaphoreSlim _signal = new(0);

    public ValueTask QueueBackgroundWorkItemAsync(T workItem, CancellationToken cancellationToken = default)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        _workItems.Enqueue(workItem);
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _signal.WaitAsync(cancellationToken);
        _workItems.TryDequeue(out var workItem);
        return workItem!;
    }
}