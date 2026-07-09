namespace SengokuScroll.Domain.Evaluators;

public abstract class EvaluatorBase
{
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
