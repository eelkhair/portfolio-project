FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
EXPOSE 80
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HealthChecks/JobBoard.WebStatus/JobBoard.WebStatus.csproj", "HealthChecks/JobBoard.WebStatus/"]
RUN dotnet restore "HealthChecks/JobBoard.WebStatus/JobBoard.WebStatus.csproj"
COPY . .
WORKDIR "/src/HealthChecks/JobBoard.WebStatus"
RUN dotnet build "JobBoard.WebStatus.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "JobBoard.WebStatus.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobBoard.WebStatus.dll"]
