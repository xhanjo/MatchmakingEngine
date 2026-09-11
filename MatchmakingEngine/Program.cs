using FluentValidation;
using MatchmakingEngine.Application.Configuration;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Auth;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.HostedServices;
using MatchmakingEngine.Hubs;
using MatchmakingEngine.Infrastructure.Auth;
using MatchmakingEngine.Infrastructure.Repositories;
using MatchmakingEngine.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using MatchmakingEngine.Application.Application.Behaviors;
using MatchmakingEngine.Infrastructure.Data;
using MatchmakingEngine.Middlewares;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, loggerConfig) =>
{
    loggerConfig
        .WriteTo.Console()
        .WriteTo.File("logs/matchmaking-.txt", rollingInterval: RollingInterval.Day)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500",
            "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IPartyRepository, PartyRepository>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IBackgroundJobService, HangfireJobService>();
builder.Services.AddSingleton<ILeaderboardService, LeaderboardService>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer is missing in configuration.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT:Audience is missing in configuration.");
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT:Key is missing in configuration.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/matchmaking"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(ValidationBehavior<,>).Assembly
    );
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(ValidationBehavior<,>).Assembly);

var redisConfiguration = builder.Configuration["Redis:Configuration"]
    ?? throw new InvalidOperationException("Redis:Configuration is missing in appsettings.json.");

builder.Services.AddSignalR().AddStackExchangeRedis(redisConfiguration);
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICacheService, TwoLevelCacheService>();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConfiguration)
);

builder.Services.AddSingleton<IMatchmakingQueue, MatchmakingQueue>();

var runWorker = builder.Configuration.GetValue("RunMatchmakingWorker", true);
if (runWorker)
{
    builder.Services.AddHostedService<MatchmakingWorker>();
    builder.Services.AddHostedService<MatchCleanupWorker>();
    builder.Services.AddHostedService<LeaderboardSeederWorker>();
}

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection string is missing.");

builder.Services.AddHealthChecks()
    .AddNpgSql(dbConnectionString, name: "PostgreSQL")
    .AddRedis(redisConfiguration, name: "Redis");

builder.Services.AddDbContext<MatchmakingDbContext>(options =>
    options.UseNpgsql(dbConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    })
);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
    options.InstanceName = "Matchmaking_";
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30, 
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHangfire(config => config
.UseSimpleAssemblyNameTypeSerializer()
.UseRecommendedSerializerSettings()
.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(dbConnectionString)));

builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MatchmakingHub>("/hubs/matchmaking");

app.MapHealthChecks("/health");


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MatchmakingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int retries = 5;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Attempting to apply database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrated successfully!");
            break;
        }
        catch (Exception)
        {
            retries--;
            logger.LogWarning("Database is not ready yet. Retrying in 2 seconds... ({Retries} retries left)", retries);
            if (retries == 0) throw;
            Thread.Sleep(2000);
        }
    }


    var playerRepo = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    var existingAdmin = await playerRepo.GetByUsernameAsync("admin");
    if (existingAdmin == null)
    {
        try
        {
            var admin = new MatchmakingEngine.Domain.Player
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = MatchmakingEngine.Domain.PlayerRole.Admin,
                Region = MatchmakingEngine.Domain.PlayerRegion.EuWest,
                Mmr = 9999
            };
            await playerRepo.AddAsync(admin);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            Console.WriteLine("SuperAdmin успішно створений!");
        }
        catch (DbUpdateException)
        {
            Console.WriteLine("SuperAdmin вже існує в БД.");
        }
    }
}

app.MapGet("/", () => "Matchmaking Engine API is running!");

app.Run();

