using MatchmakingEngine.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace MatchmakingEngine.Tests.Helpers;

// [Collection] гарантує, що Docker-контейнер підніметься ОДИН раз для всіх тестів, 
// а не буде перестворюватись для кожного тестового файлу (що було б дуже довго).
[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient Client; 

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(); // Цей клієнт дозволить робити API запити до серверу
    }

    protected MatchmakingDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();
    }
}