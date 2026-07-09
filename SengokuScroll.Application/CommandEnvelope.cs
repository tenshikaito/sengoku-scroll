using SengokuScroll.Application.Models;
using SengokuScroll.Domain;

namespace SengokuScroll.Application;


public class CommandEnvelope(ICommand command)
{
    public ICommand Command { get; } = command;

    public TaskCompletionSource<GameResult> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}