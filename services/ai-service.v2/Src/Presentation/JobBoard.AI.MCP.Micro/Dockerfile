FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 3334

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Src/Presentation/JobBoard.AI.API/Nuget.Config", "."]
COPY ["Src/Presentation/JobBoard.AI.MCP.Micro/JobBoard.AI.MCP.Micro.csproj", "Src/Presentation/JobBoard.AI.MCP.Micro/"]
COPY ["Src/Infrastructure/JobBoard.AI.Infrastructure.Configuration/JobBoard.AI.Infrastructure.Configuration.csproj", "Src/Infrastructure/JobBoard.AI.Infrastructure.Configuration/"]
COPY ["Src/Core/JobBoard.AI.Application/JobBoard.AI.Application.csproj", "Src/Core/JobBoard.AI.Application/"]
COPY ["Src/Core/JobBoard.AI.Domain/JobBoard.AI.Domain.csproj", "Src/Core/JobBoard.AI.Domain/"]
COPY ["Src/Infrastructure/JobBoard.AI.Infrastructure.Persistence/JobBoard.AI.Infrastructure.Persistence.csproj", "Src/Infrastructure/JobBoard.AI.Infrastructure.Persistence/"]
COPY ["Src/Infrastructure/JobBoard.AI.Infrastructure.Dapr/JobBoard.AI.Infrastructure.Dapr.csproj", "Src/Infrastructure/JobBoard.AI.Infrastructure.Dapr/"]
COPY ["Src/Infrastructure/JobBoard.AI.Infrastructure.Diagnostics/JobBoard.AI.Infrastructure.Diagnostics.csproj", "Src/Infrastructure/JobBoard.AI.Infrastructure.Diagnostics/"]
COPY ["Src/Infrastructure/JobBoard.AI.Infrastructure.AI/JobBoard.AI.Infrastructure.AI.csproj", "Src/Infrastructure/JobBoard.AI.Infrastructure.AI/"]
COPY ["Src/Infrastructure/JobBoard.AI.Infrastructure.HttpClients/JobBoard.AI.Infrastructure.HttpClients.csproj", "Src/Infrastructure/JobBoard.AI.Infrastructure.HttpClients/"]
RUN dotnet restore "./Src/Presentation/JobBoard.AI.MCP.Micro/JobBoard.AI.MCP.Micro.csproj" --configfile "Nuget.Config"
COPY . .
WORKDIR "/src/Src/Presentation/JobBoard.AI.MCP.Micro"
RUN dotnet build "./JobBoard.AI.MCP.Micro.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JobBoard.AI.MCP.Micro.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobBoard.AI.MCP.Micro.dll"]
