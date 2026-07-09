using SengokuScroll.Application.Models;
using SengokuScroll.Domain;
using System.Threading.Channels;

namespace SengokuScroll.Application;

public class CommandQueue
{
    private readonly Channel<CommandEnvelope> channel = Channel.CreateUnbounded<CommandEnvelope>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public async Task<GameResult> EnqueueAsync(ICommand cmd)
    {
        var env = new CommandEnvelope(cmd);
        // best-effort write; if write fails we set exception
        if (!channel.Writer.TryWrite(env))
        {
            var tcs = new TaskCompletionSource<GameResult>();
            tcs.SetException(new InvalidOperationException("Failed to enqueue command"));
            return await tcs.Task;
        }

        return await env.Tcs.Task;
    }

    public ChannelReader<CommandEnvelope> Reader => channel.Reader;

    /// <summary>
    /// Mark the queue as complete: no more items will be written. The reader will finish
    /// after all already-enqueued items are consumed.
    /// </summary>
    public void Complete() => channel.Writer.Complete();
}
