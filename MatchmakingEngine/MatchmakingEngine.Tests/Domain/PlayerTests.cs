using FluentAssertions;
using MatchmakingEngine.Domain;
using Xunit;

namespace MatchmakingEngine.MatchmakingEngine.Tests.Domain;

public class PlayerTests
{
    [Fact]
    public void RecordLoss_WhenMmrLessThanChange_ShouldClampToZero()
    {
        var player = new Player
        {
            Username = "TestPlayer",
            Mmr = 10.0
        };

        player.RecordLoss(MmrChange: 25.0);

        player.Mmr.Should().Be(0);
    }

    [Fact]
    public void RecordWin_ShouldIncreaseMmr()
    {
        var player = new Player { Mmr = 1000.0 };

        player.RecordWin(MmrChange: 25.0);

        player.Mmr.Should().Be(1025.0);
    }

    [Fact]
    public void RecordLoss_WhenMmrGreaterThanChange_ShouldSubtract()
    {
        var player = new Player { Mmr = 1000.0 };

        player.RecordLoss(MmrChange: 25.0);
        
        player.Mmr.Should().Be(975.0);
    }
}
