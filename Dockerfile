# Multi-stage Dockerfile that builds the React frontend + .NET backend
# and runs them as a single container serving the SPA + API on $PORT (default 5090).

# ---------- Stage 1: build the React frontend ----------
FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci --no-audit --no-fund
COPY frontend ./
# vite.config.js writes to ../backend/ProjectHub.Api/wwwroot. Override here so
# the build output stays inside the frontend tree and can be COPY'd in stage 2.
RUN npx vite build --outDir dist --emptyOutDir

# ---------- Stage 2: restore + build + publish the .NET backend ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

# Copy project files first so restore can be cached separately from sources.
COPY ProjectHub.slnx ./
COPY backend/ProjectHub.Domain/ProjectHub.Domain.csproj backend/ProjectHub.Domain/
COPY backend/ProjectHub.Services/ProjectHub.Services.csproj backend/ProjectHub.Services/
COPY backend/ProjectHub.Persistence/ProjectHub.Persistence.csproj backend/ProjectHub.Persistence/
COPY backend/ProjectHub.Api/ProjectHub.Api.csproj backend/ProjectHub.Api/

RUN dotnet restore backend/ProjectHub.Api/ProjectHub.Api.csproj

# Now copy the rest of the .NET sources.
COPY backend/ProjectHub.Domain backend/ProjectHub.Domain
COPY backend/ProjectHub.Services backend/ProjectHub.Services
COPY backend/ProjectHub.Persistence backend/ProjectHub.Persistence
COPY backend/ProjectHub.Api backend/ProjectHub.Api
# Drop any local wwwroot - we copy the freshly built one from stage 1.
RUN rm -rf backend/ProjectHub.Api/wwwroot
COPY --from=frontend-build /src/frontend/dist backend/ProjectHub.Api/wwwroot

# Skip the npm-build target during dotnet publish: there's no Node in this stage,
# and the SPA assets are already in wwwroot.
RUN dotnet publish backend/ProjectHub.Api/ProjectHub.Api.csproj \
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

COPY --from=backend-build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5090 \
    Server__Port=5090 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false

EXPOSE 5090

# Mount-points: keep the JSON store and any Claude config outside the image.
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "ProjectHub.Api.dll"]
