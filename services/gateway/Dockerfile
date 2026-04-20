FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Download the MaxMind GeoLite2 City database in an isolated stage so the
# license key never ends up in a final-image layer. The mmdb is ~65 MB and
# gets copied into the runner stage only. If no license key is provided at
# build time (local dev) the stage writes a .missing sentinel and GeoService
# logs a warning + returns empty results — callers treat as "unknown".
FROM alpine:3.20 AS mmdb
ARG MAXMIND_LICENSE_KEY
RUN apk add --no-cache curl tar && \
    mkdir -p /mmdb && \
    if [ -z "$MAXMIND_LICENSE_KEY" ]; then \
      echo "WARN: MAXMIND_LICENSE_KEY not provided — gateway will return empty geo results"; \
      touch /mmdb/GeoLite2-City.mmdb.missing; \
    else \
      curl -fsSL -o /tmp/geolite.tar.gz "https://download.maxmind.com/app/geoip_download?edition_id=GeoLite2-City&license_key=${MAXMIND_LICENSE_KEY}&suffix=tar.gz" && \
      tar -xzf /tmp/geolite.tar.gz -C /tmp && \
      mv /tmp/GeoLite2-City_*/GeoLite2-City.mmdb /mmdb/GeoLite2-City.mmdb && \
      rm -rf /tmp/geolite.tar.gz /tmp/GeoLite2-City_* && \
      ls -la /mmdb/GeoLite2-City.mmdb; \
    fi

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Nuget.Config", "."]
COPY ["Gateway.Api.csproj", ""]
RUN mkdir -p /nupkgs
RUN dotnet restore "./Gateway.Api.csproj" --configfile "Nuget.Config"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
USER root
RUN mkdir -p /app/geo && chown $APP_UID /app/geo
USER $APP_UID
COPY --from=publish /app/publish .
# mmdb path matches GeoService default (/app/geo/GeoLite2-City.mmdb).
COPY --from=mmdb --chown=$APP_UID /mmdb/ /app/geo/
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
