FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MatchmakingEngine/MatchmakingEngine.csproj", "MatchmakingEngine/"]
COPY ["MatchmakingEngine.Application/MatchmakingEngine.Application.csproj", "MatchmakingEngine.Application/"]
COPY ["MatchmakingEngine.Domain/MatchmakingEngine.Domain.csproj", "MatchmakingEngine.Domain/"]
COPY ["MatchmakingEngine.Infrastructure/MatchmakingEngine.Infrastructure.csproj", "MatchmakingEngine.Infrastructure/"]

RUN dotnet restore "MatchmakingEngine/MatchmakingEngine.csproj"

COPY . .
WORKDIR "/src/MatchmakingEngine"

RUN dotnet publish "MatchmakingEngine.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MatchmakingEngine.dll"]