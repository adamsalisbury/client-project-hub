#!/usr/bin/env bash
# Run the Vite dev server on http://localhost:5173 (proxies /api to :5090).
# Installs npm dependencies if node_modules is missing.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root/frontend"

if [[ ! -d node_modules ]]; then
    npm install
fi

exec npm run dev -- "$@"
