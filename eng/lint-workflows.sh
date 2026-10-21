#!/usr/bin/env bash
# Lints GitHub Actions workflows and Dependabot configuration: actionlint (syntax, expressions, shellcheck) and zizmor
# (security audits). Both run from images pinned by digest (docs/plan/DECISIONS.md D-051). zizmor's online audits
# (impostor commits, known-vulnerable actions) use GH_TOKEN when it is set; without it they are skipped.
# Usage: eng/lint-workflows.sh [directory ...]   (default: the repository root)
set -euo pipefail

actionlint_image='rhysd/actionlint@sha256:b1934ee5f1c509618f2508e6eb47ee0d3520686341fec936f3b79331f9315667' # 1.7.12
zizmor_image='ghcr.io/zizmorcore/zizmor@sha256:42d409dbb851d4c5d35a058cf042b828bea1e3f2884378c526eb0974780e18d9' # 1.31.1

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# Docker on Windows (Git Bash) wants a Windows path for the volume, and no MSYS path conversion of the arguments.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }
export MSYS_NO_PATHCONV=1

if [[ $# -eq 0 ]]; then set -- "$root"; fi
for dir in "$@"; do
  dir="$(cd "$dir" && pwd)"
  echo "== $dir"
  # Files are named explicitly: actionlint finds .github/workflows only inside a git repository, and a generated project
  # isn't one.
  workflows=()
  for file in "$dir"/.github/workflows/*.yml "$dir"/.github/workflows/*.yaml; do
    if [[ -f "$file" ]]; then workflows+=(".github/workflows/${file##*/}"); fi
  done
  if [[ ${#workflows[@]} -eq 0 ]]; then
    echo "::error::$dir has no workflow in .github/workflows" >&2
    exit 1
  fi
  docker run --rm -v "$(native "$dir"):/repo" -w /repo "$actionlint_image" -color "${workflows[@]}"
  docker run --rm -e GH_TOKEN -v "$(native "$dir"):/repo" -w /repo "$zizmor_image" --strict-collection .github
done
