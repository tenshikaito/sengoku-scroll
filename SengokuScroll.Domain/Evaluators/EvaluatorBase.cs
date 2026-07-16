namespace SengokuScroll.Domain.Evaluators;

/// <summary>评估器基类：按序执行多条规则，遇首个失败即短路返回。</summary>
public abstract class EvaluatorBase
{
    /// <summary>依次执行规则委托列表，全部通过则返回成功。</summary>
    protected static GameResult Evaluate(IEnumerable<Func<GameResult>> args)
    {
        foreach (var rule in args)
        {
            var result = rule();

            if (!result)
                return result;
        }

        return GameResult.Ok();
    }
}
