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
src/
  ProjectHub.Domain/        Domain models + DTOs (no dependencies)
  ProjectHub.Persistence/   JSON-backed IClaudeDataProvider
  ProjectHub.Services/      Service layer + background worker
    Runner/                 IClaudeRunner (subprocess invoker)
    Workers/                ClaudeJobWorker · ProjectPromptBuilder
    Storage/                IClaudeDataProvider · queue
  ProjectHub.Api/           .NET 10 web app - API + SPA host
    Controllers/            api/clients · api/projects · api/claude · …
    ClientApp/              Vite + React 18 SPA
    wwwroot/                SPA build output
tests/
  ProjectHub.Tests/         xUnit
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

```bash
dotnet run --project src/ProjectHub.Api
```

Default URL is `http://0.0.0.0:5090`. The React client lives in
`src/ProjectHub.Api/ClientApp`; build it once with:

```bash
cd src/ProjectHub.Api/ClientApp && npm install && npm run build
```

The Claude Code CLI must be on `PATH` and authenticated. The wrapper invokes
it with `--dangerously-skip-permissions` so no interactive approval is required.

## Tests

```bash
dotnet test
```

## Licence

MIT — see [LICENSE](LICENSE).
