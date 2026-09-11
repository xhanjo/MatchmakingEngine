using MatchmakingEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Tests.Helpers;

public static class TestDbContextFactory
{
    public static MatchmakingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<MatchmakingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new MatchmakingDbContext(options);
    }
}
