#!/usr/bin/env bash
# API contract check (docs/plan/DECISIONS.md D-061): compares docs/openapi/v1.json, the committed OpenAPI document of the
# Api host (OpenApiContractTests keeps it current), with the base revision's and fails on a breaking change (oasdiff,
# ERR level), from an image pinned by digest like eng/lint-workflows.sh. Nothing leaves the machine. The base is the
# branch point from origin/main; on main itself, the previous commit (a merge's first parent). A breaking change made on
# purpose is accepted by a line of eng/openapi-breaking-accepted.txt holding oasdiff's message for it, e.g.
#   GET /api/v1/products/{id} removed the success response with the status '200'
# Usage: eng/check-openapi.sh [base-revision]
set -euo pipefail

oasdiff_image='tufin/oasdiff@sha256:6263a96dd2ef0726c54e21fea9b8e1607eac4841add0079324b424c1f52b819c' # v1.33.0

cd "$(dirname "${BASH_SOURCE[0]}")/.."
document=docs/openapi/v1.json

# Docker on Windows (Git Bash) wants a Windows path for the volume, and no MSYS path conversion of the arguments.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }
export MSYS_NO_PATHCONV=1

base="${1:-}"
if [[ -z "$base" ]]; then
  base=$(git merge-base HEAD origin/main)
  if [[ "$base" == "$(git rev-parse HEAD)" ]]; then base=HEAD^1; fi
fi
if ! git cat-file -e "$base:$document" 2> /dev/null; then
  echo "== $document doesn't exist at $(git rev-parse --short "$base"): nothing to compare yet"
  exit 0
fi

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
git show "$base:$document" > "$work/base.json"
cp "$document" "$work/revision.json"
cp eng/openapi-breaking-accepted.txt "$work/accepted.txt"

echo "== Breaking changes in $document since $(git rev-parse --short "$base")"
docker run --rm -v "$(native "$work"):/specs:ro" "$oasdiff_image" \
  breaking --fail-on ERR --err-ignore /specs/accepted.txt /specs/base.json /specs/revision.json
