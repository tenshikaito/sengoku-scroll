using SengokuScroll.Application.Models;
using SengokuScroll.Domain;
using System.Threading.Channels;

namespace SengokuScroll.Application;

/// <summary>异步命令队列：单读者通道，写入方等待处理结果。</summary>
public class CommandQueue
{
    private readonly Channel<CommandEnvelope> channel = Channel.CreateUnbounded<CommandEnvelope>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    /// <summary>入队命令并等待处理器完成。</summary>
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

    /// <summary>标记队列完成：不再写入，读者消费完剩余项后结束。</summary>
    public void Complete() => channel.Writer.Complete();
}
