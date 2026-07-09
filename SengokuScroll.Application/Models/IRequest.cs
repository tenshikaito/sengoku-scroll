using SengokuScroll.Application.Contexts;
using SengokuScroll.Domain;

namespace SengokuScroll.Application.Models;

public interface IRequest
{
}

public interface IRequest<out TResult> : IRequest
{
}

public interface ICommand : IRequest
{

}

public interface IQuery : IRequest
{

}

public interface IQuery<out TResult> : IRequest<TResult>
{

}

public interface IRequestHandler
{

}

public interface IRequestHandler<TRequest> : IRequestHandler where TRequest : IRequest
{
    GameResult Handle(TRequest request, IGameRequestContext context);
}

public interface IRequestHandler<TRequest, TResult> : IRequestHandler where TRequest : IRequest
{
    GameResult<TResult> Handle(TRequest request, IGameRequestContext context);
}
