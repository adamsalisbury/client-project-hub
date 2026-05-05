# Multi-stage Dockerfile that builds the React client + .NET API
# and runs them as a single container serving the SPA + API on $PORT (default 5090).

# ---------- Stage 1: build the React client ----------
FROM node:22-alpine AS client-build
WORKDIR /src/ClientApp
COPY src/ProjectHub.Api/ClientApp/package.json src/ProjectHub.Api/ClientApp/package-lock.json* ./
RUN npm ci --no-audit --no-fund
COPY src/ProjectHub.Api/ClientApp ./
# Vite builds into ../wwwroot via vite.config.js. Mirror that here.
RUN npm run build && ls -la ../wwwroot

# ---------- Stage 2: restore + build + publish the .NET API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src

# Copy the project files first so restore can be cached separately from sources.
COPY ProjectHub.slnx ./
COPY src/ProjectHub.Domain/ProjectHub.Domain.csproj src/ProjectHub.Domain/
COPY src/ProjectHub.Services/ProjectHub.Services.csproj src/ProjectHub.Services/
COPY src/ProjectHub.Persistence/ProjectHub.Persistence.csproj src/ProjectHub.Persistence/
COPY src/ProjectHub.Api/ProjectHub.Api.csproj src/ProjectHub.Api/

RUN dotnet restore src/ProjectHub.Api/ProjectHub.Api.csproj

# Now copy the rest of the sources (excluding ClientApp - it comes from stage 1).
COPY src/ProjectHub.Domain src/ProjectHub.Domain
COPY src/ProjectHub.Services src/ProjectHub.Services
COPY src/ProjectHub.Persistence src/ProjectHub.Persistence
COPY src/ProjectHub.Api src/ProjectHub.Api
# Drop any local ClientApp/wwwroot - we rebuild both inside the image.
RUN rm -rf src/ProjectHub.Api/ClientApp src/ProjectHub.Api/wwwroot
COPY --from=client-build /src/wwwroot src/ProjectHub.Api/wwwroot

# Disable the npm-build target during dotnet publish: there's no Node in this stage,
# and the SPA assets are already in wwwroot.
RUN dotnet publish src/ProjectHub.Api/ProjectHub.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:SkipClientBuild=true \
    --no-restore

# ---------- Stage 3: minimal runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# git is needed by the diff/history endpoints; node + claude CLI
# are only required if you want the runner to actually invoke Claude.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=dotnet-build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5090 \
    Server__Port=5090 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false

EXPOSE 5090

# Mount-points: keep the JSON store and any Claude config outside the image.
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "ProjectHub.Api.dll"]
