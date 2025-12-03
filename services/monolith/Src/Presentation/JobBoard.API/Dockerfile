# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Src/Presentation/JobBoard.API/Nuget.Config", "."]
COPY ["Src/Presentation/JobBoard.API/JobBoard.API.csproj", "Src/Presentation/JobBoard.API/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Configuration/JobBoard.Infrastructure.Configuration.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Configuration/"]
COPY ["Src/Core/JobBoard.Application/JobBoard.Application.csproj", "Src/Core/JobBoard.Application/"]
COPY ["Src/Core/JobBoard.Domain/JobBoard.Domain.csproj", "Src/Core/JobBoard.Domain/"]
COPY ["Src/Core/JobBoard.IntegrationEvents/JobBoard.IntegrationEvents.csproj", "Src/Core/JobBoard.IntegrationEvents/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Dapr/JobBoard.Infrastructure.Dapr.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Dapr/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Diagnostics/JobBoard.Infrastructure.Diagnostics.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Diagnostics/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Outbox/JobBoard.Infrastructure.Outbox.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Outbox/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Persistence/JobBoard.Infrastructure.Persistence.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Persistence/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Smtp/JobBoard.Infrastructure.Smtp.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Smtp/"]
RUN dotnet restore "./Src/Presentation/JobBoard.API/JobBoard.API.csproj" --configfile "Nuget.Config"
COPY . .
WORKDIR "/src/Src/Presentation/JobBoard.API"
RUN dotnet build "./JobBoard.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JobBoard.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobBoard.API.dll"]