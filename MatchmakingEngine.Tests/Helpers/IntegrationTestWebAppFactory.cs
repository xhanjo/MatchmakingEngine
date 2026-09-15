using MatchmakingEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;


namespace MatchmakingEngine.Tests.Helpers;

// Цей клас автоматично підніме Docker-контейнер перед тестами
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("matchmaking_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();
    
    public async Task InitializeAsync()
    {
        // Запускаємо обидва контейнери паралельно для максимальної швидкості
        await Task.WhenAll(_dbContainer.StartAsync(), _redisContainer.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        // Зупиняємо обидва контейнери після завершення тестів
        await Task.WhenAll(_dbContainer.StopAsync(), _redisContainer.StopAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // === ПІДМІНА POSTGRESQL ===
            // 1. Знаходимо стандартне підключення до БД і видаляємо його
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MatchmakingDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 2. Додаємо нове підключення, яке дивиться на наш щойно піднятий Docker-контейнер
            services.AddDbContext<MatchmakingDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
            
            // === ПІДМІНА REDIS ===
            // 1. Замінюємо IConnectionMultiplexer (використовується чергами матчмейкінгу та SignalR)
            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null)
            {
                services.Remove(redisDescriptor);
            }
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
            // 2. Оновлюємо рядок підключення для IDistributedCache (дворівневий кеш TwoLevelCacheService)
            services.Configure<RedisCacheOptions>(options =>
            {
                options.Configuration = _redisContainer.GetConnectionString();
            });
        });
    }
}