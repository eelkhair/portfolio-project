FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Src/Presentation/JobBoard.API/JobBoard.API.csproj", "Src/Presentation/JobBoard.API/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Configuration/JobBoard.Infrastructure.Configuration.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Configuration/"]
COPY ["Src/Core/JobBoard.Application/JobBoard.Application.csproj", "Src/Core/JobBoard.Application/"]
COPY ["Src/Core/JobBoard.Domain/JobBoard.Domain.csproj", "Src/Core/JobBoard.Domain/"]
COPY ["Src/Core/JobBoard.IntegrationEvents/JobBoard.IntegrationEvents.csproj", "Src/Core/JobBoard.IntegrationEvents/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Diagnostics/JobBoard.Infrastructure.Diagnostics.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Diagnostics/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Outbox/JobBoard.Infrastructure.Outbox.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Outbox/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Persistence/JobBoard.Infrastructure.Persistence.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Persistence/"]
COPY ["Src/Infrastructure/JobBoard.Infrastructure.Smtp/JobBoard.Infrastructure.Smtp.csproj", "Src/Infrastructure/JobBoard.Infrastructure.Smtp/"]
RUN dotnet restore "Src/Presentation/JobBoard.API/JobBoard.API.csproj"
COPY . .
WORKDIR "/src/Src/Presentation/JobBoard.API"
RUN dotnet build "./JobBoard.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JobBoard.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobBoard.API.dll"]
