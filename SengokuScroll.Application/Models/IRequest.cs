using SengokuScroll.Application.Contexts;
using SengokuScroll.Domain;

namespace SengokuScroll.Application.Models;

/// <summary>命令/查询与处理器契约。</summary>
public interface IRequest
{
}

public interface IRequest<out TResult> : IRequest
{
}

/// <summary>会改变游戏世界的写操作（移动、攻击等）。</summary>
public interface ICommand : IRequest
{

}

/// <summary>只读查询（不修改世界状态）。</summary>
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
    /// <summary>处理命令并返回成功/业务错误码。</summary>
    GameResult Handle(TRequest request, IGameRequestContext context);
}

public interface IRequestHandler<TRequest, TResult> : IRequestHandler where TRequest : IRequest
{
    /// <summary>处理查询并返回带载荷的结果。</summary>
    GameResult<TResult> Handle(TRequest request, IGameRequestContext context);
}
