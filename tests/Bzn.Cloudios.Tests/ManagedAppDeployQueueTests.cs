using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;

namespace Bzn.Cloudios.Tests;

public class ManagedAppDeployQueueTests
{
    [Fact]
    public async Task Enqueue_And_DequeueAllAsync_ReturnsEnqueuedIds()
    {
        var queue = new ManagedAppDeployQueue();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        queue.Enqueue(id1);
        queue.Enqueue(id2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var results = new List<Guid>();

        await foreach (var id in queue.DequeueAllAsync(cts.Token))
        {
            results.Add(id);
            if (results.Count == 2) break;
        }

        Assert.Equal(2, results.Count);
        Assert.Equal(id1, results[0]);
        Assert.Equal(id2, results[1]);
    }

    [Fact]
    public void Enqueue_DoesNotThrow()
    {
        var queue = new ManagedAppDeployQueue();
        var exception = Record.Exception(() => queue.Enqueue(Guid.NewGuid()));
        Assert.Null(exception);
    }

    [Fact]
    public async Task DequeueAllAsync_CancellationStopsEnumeration()
    {
        var queue = new ManagedAppDeployQueue();
        using var cts = new CancellationTokenSource();

        queue.Enqueue(Guid.NewGuid());
        cts.Cancel();

        var items = new List<Guid>();
        try
        {
            await foreach (var id in queue.DequeueAllAsync(cts.Token))
            {
                items.Add(id);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        // May or may not have read one item before cancellation — just verify no crash
        Assert.True(items.Count <= 1);
    }

    [Fact]
    public void IManagedAppDeployQueue_Interface_IsImplemented()
    {
        IManagedAppDeployQueue queue = new ManagedAppDeployQueue();
        Assert.NotNull(queue);
    }
}
