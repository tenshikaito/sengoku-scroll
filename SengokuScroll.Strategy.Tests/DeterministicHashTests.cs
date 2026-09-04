using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Tests;

public class DeterministicHashTests
{
    [Fact]
    public void Combine_IntegerVector_MatchesStableFnvValue()
        => Assert.Equal(
            1292194974,
            DeterministicHash.Combine(15600101, 1, 1560, 1, 1, 80));

    [Fact]
    public void Combine_StringAndDate_MatchesStableFnvValue()
        => Assert.Equal(
            1382477605,
            DeterministicHash.Combine("mini_kanto", 1560, 1, 1));
}
