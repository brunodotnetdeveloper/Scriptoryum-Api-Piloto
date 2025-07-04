using System.Threading;

namespace Scriptoryum.Api.Application.Utils;

public interface IBackgroundTaskQueue<T>
{
    ValueTask QueueBackgroundWorkItemAsync(T workItem, CancellationToken cancellationToken = default);
    ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default);
}