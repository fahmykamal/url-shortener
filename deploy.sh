#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

port="$(grep -E '^API_PORT=' .env 2>/dev/null | tail -n1 | cut -d= -f2- || true)"
port="${port:-5076}"
state_file=".deployed-rev"

git fetch --quiet origin main
remote_rev="$(git rev-parse origin/main)"

if [ -f "$state_file" ] && [ "$(cat "$state_file")" = "$remote_rev" ]; then
    exit 0
fi

echo "Deploying ${remote_rev}"
git merge --ff-only origin/main

docker compose up -d --build
docker image prune -f

for _ in $(seq 1 30); do
    if curl -fsS "http://localhost:${port}/" >/dev/null 2>&1; then
        echo "$remote_rev" > "$state_file"
        echo "Deployed ${remote_rev}"
        exit 0
    fi
    sleep 2
done

echo "Health check failed on port ${port}" >&2
exit 1
