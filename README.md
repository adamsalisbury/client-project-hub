# client-project-hub

A .NET 10 web app that wraps the Claude Code CLI behind a project-aware
JSON API and serves a React (Vite) single-page app for interactive use.

The domain model is **clients → projects → tickets**:

- A **client** groups related projects together and may carry shared
  knowledge entries that are folded into every prompt for projects beneath it.
- A **project** pins a working directory; every Claude invocation in that
  project runs from there with the full prior message history replayed
  as context.
- A **ticket** is a piece of work attached to a project. Tickets, knowledge
  entries, agent personas and prior turns can all be toggled on and off per
  project via the memory-tweaking tab.

Messages are queued, processed by a hosted background worker, and stored
in a JSON-backed data provider behind an `IClaudeDataProvider` interface,
so the storage layer can be swapped (for example, for SQL) later.

## Layout

```
backend/                          .NET 10 backend
  ProjectHub.Domain/              Domain models + DTOs (no dependencies)
  ProjectHub.Persistence/         JSON-backed IClaudeDataProvider
  ProjectHub.Services/            Service layer + background worker
    Runner/                       IClaudeRunner (subprocess invoker)
    Workers/                      ClaudeJobWorker · ProjectPromptBuilder
    Storage/                      IClaudeDataProvider · queue
  ProjectHub.Api/                 Web app - controllers + SPA host
    Controllers/                  api/clients · api/projects · api/claude · …
    wwwroot/                      SPA build output (gitignored)
  tests/
    ProjectHub.Tests/             xUnit
frontend/                         React 18 + Vite SPA (project root)
  src/                            components, router, api client
  public/
ProjectHub.slnx                   solution file across the .NET projects
Dockerfile                        multi-stage: builds frontend, then backend
```

## Endpoints

```
GET  /                                  React SPA
GET  /api/clients                       list clients
POST /api/clients                       (body: { name })
GET  /api/clients/{id}/projects         list projects under a client
GET  /api/clients/{id}/knowledge        client-level knowledge entries
POST /api/projects                      (body: { name, workingDirectory, clientId })
GET  /api/projects
GET  /api/projects/{id}/history
PUT  /api/projects/{id}/client          reassign a project to a different client
POST /api/claude                        (body: { projectId, message, kind })  → 202 + guid
GET  /api/claude/{id}                   status / result
GET  /api/filesystem/browse?path=...    directory listing for the path picker
```

Mutating endpoints require an antiforgery token. The SPA reads the
`XSRF-TOKEN` cookie set on the first GET and sends it back as the
`X-XSRF-TOKEN` header. CLI consumers must do the same.

## Running

The simplest way — let the API project's MSBuild target invoke the
frontend build for you:

```bash
dotnet run --project backend/ProjectHub.Api
```

That runs `npm install` (if `frontend/node_modules` is missing) and
`npm run build` against `frontend/`, which writes the bundle into
`backend/ProjectHub.Api/wwwroot/`. Default URL is `http://0.0.0.0:5090`.

To skip the implicit frontend build (e.g. when you've built it manually or
are iterating with `vite dev`):

```bash
dotnet run --project backend/ProjectHub.Api /p:SkipClientBuild=true
```

For frontend-focused work, run Vite's dev server with proxying to the API:

```bash
# terminal 1 - the .NET API
dotnet run --project backend/ProjectHub.Api /p:SkipClientBuild=true

# terminal 2 - the Vite dev server with HMR (proxies /api → :5090)
cd frontend
npm install      # first time only
npm run dev
```

The Claude Code CLI must be on `PATH` and authenticated. The wrapper invokes
it with `--dangerously-skip-permissions` so no interactive approval is required.

## Tests

```bash
dotnet test
```

## Docker

```bash
docker build -t project-hub .
docker run --rm -p 5090:5090 -v $PWD/data:/app/data project-hub
```

## Licence

MIT — see [LICENSE](LICENSE).
