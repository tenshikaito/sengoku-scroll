using SengokuScroll.Application.Models;
using SengokuScroll.Domain;

namespace SengokuScroll.Application;


/// <summary>命令信封：携带命令与完成源，供异步队列等待结果。</summary>
public class CommandEnvelope(ICommand command)
{
    public ICommand Command { get; } = command;

    public TaskCompletionSource<GameResult> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}