# Multi-stage Dockerfile that builds the React frontend + .NET backend and
# produces a single self-contained runtime image with the Claude Code CLI
# pre-installed. Build and run with:
#
#   docker build -t client-project-hub .
#   docker run --rm -p 5090:5090 \
#       -v ~/clients:/clients \
#       -v $PWD/data:/app/data \
#       -v ~/.claude:/root/.claude \
#       client-project-hub
#
# Volumes:
#   /app/data       - JSON-backed job/client/project/ticket store (writable).
#   /clients        - host directory of project repos. The path browser opens
#                     here by default; bind-mount your host ~/clients (or
#                     similar) to /clients. The Claude runner edits files in
#                     these working directories, so the mount needs to be
#                     read+write.
#   /root/.claude   - Claude Code CLI auth/session state. Without this the
#                     CLI inside the container will need to log in interactively.

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

# ---------- Stage 3: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# System packages:
#   git           - used by the file-history / diff endpoints
#   ca-certs      - for HTTPS to npm registry + Anthropic API
#   curl          - bootstrap NodeSource repo
#   nodejs (22.x) - required by the Claude Code CLI
# Then install the Claude Code CLI globally so `claude` is on PATH for the runner.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates curl gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && npm install -g @anthropic-ai/claude-code \
    && apt-get purge -y --auto-remove curl gnupg \
    && rm -rf /var/lib/apt/lists/* /root/.npm

COPY --from=backend-build /app/publish .

# Pre-create the volume targets so the container has them on first start, even
# without a bind mount. /clients is where the host's projects directory should
# be mounted; chmod 0777 keeps things permissive in case the bind-mount source
# files are owned by a non-root host user (the container runs as root by default
# under Linux Docker, so this is mainly belt-and-braces for non-mounted dev runs).
RUN mkdir -p /app/data /clients /root/.claude \
    && chmod 0777 /clients

ENV ASPNETCORE_ENVIRONMENT=Production \
    Server__Port=5090 \
    JsonDataProvider__FilePath=/app/data/jobs.json \
    Filesystem__BrowseRoot=/clients \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false

EXPOSE 5090

# Mount-points: keep the JSON store, the host's project directory, and the
# Claude CLI auth state outside the image so they survive container rebuilds.
VOLUME ["/app/data", "/clients", "/root/.claude"]

ENTRYPOINT ["dotnet", "ProjectHub.Api.dll"]
