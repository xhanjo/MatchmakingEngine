using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MatchmakingEngine.Tests.Integration;

public class TwoLevelCacheServiceIntegrationTests : BaseIntegrationTest
{
    private readonly ICacheService _cacheService;

    public TwoLevelCacheServiceIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        _cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldStoreAndRetrieveValueFromCache()
    {
        var cacheKey = $"test_player_{Guid.NewGuid()}";
        var expectedValue = "Grandmaster";
        var factoryCallCount = 0;

        async Task<string> ValueFactory()
        {
            factoryCallCount++;
            return await Task.FromResult(expectedValue);
        }

        // Act 1: Перший виклик — значення має взятися з фабрики і записатися в пам'ять + Redis
        var firstResult = await _cacheService.GetOrCreateAsync(cacheKey, ValueFactory, TimeSpan.FromMinutes(5));

        // Act 2: Другий виклик — фабрика вже НЕ повинна викликатися, значення має взятися з кешу
        var secondResult = await _cacheService.GetOrCreateAsync(cacheKey, ValueFactory, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(expectedValue, firstResult);
        Assert.Equal(expectedValue, secondResult);
        Assert.Equal(1, factoryCallCount);
    }
}