using MatchmakingEngine.Data;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Tests.Helpers;

public static class TestDbContextFactory
{
    public static MatchmakingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<MatchmakingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MatchmakingDbContext(options);
    }
}
