# ---- base runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Expose the port your app will bind to (optional)
EXPOSE 8080
# Optional: set the listening URL inside container
ENV ASPNETCORE_URLS=http://+:8080

# ---- build image ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore first (cache-friendly)
COPY JobBoard.WebStatus/JobBoard.WebStatus.csproj JobBoard.WebStatus/
RUN dotnet restore "JobBoard.WebStatus/JobBoard.WebStatus.csproj"

# Copy the rest of the source
COPY . .

# Build
WORKDIR /src/JobBoard.WebStatus
RUN dotnet build "JobBoard.WebStatus.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ---- publish image ----
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "JobBoard.WebStatus.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ---- final image ----
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobBoard.WebStatus.dll"]
