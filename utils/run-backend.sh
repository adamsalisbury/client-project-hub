#!/usr/bin/env bash
# Run the ProjectHub API on http://localhost:5090.
# Any extra args (e.g. --no-restore, --launch-profile X) are forwarded to dotnet run.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root/backend/ProjectHub.Api"

exec dotnet run "$@"
