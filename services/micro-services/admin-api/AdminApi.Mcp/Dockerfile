FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 3334

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Nuget.Config", "."]
COPY ["AdminApi.Mcp/AdminApi.Mcp.csproj", "AdminApi.Mcp/"]
COPY ["AdminApi.Core/AdminApi.Core.csproj", "AdminApi.Core/"]
COPY ["AdminAPI.Contracts/AdminAPI.Contracts.csproj", "AdminAPI.Contracts/"]
RUN mkdir -p /nupkgs
RUN dotnet restore "./AdminApi.Mcp/AdminApi.Mcp.csproj" --configfile "Nuget.Config"
COPY . .
WORKDIR "/src/AdminApi.Mcp"
RUN dotnet build "./AdminApi.Mcp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AdminApi.Mcp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdminApi.Mcp.dll"]
