#!/usr/bin/env bash
# Mutation testing (docs/plan/DECISIONS.md D-063): Stryker.NET changes the core abstractions and the modules' domain code
# one small edit at a time and runs tests/NovaCore.Domain.Tests against each change; a change no test notices survives.
# The score is a report, nothing fails on it yet (--break-at 0). The domain test project is the fast one: Stryker's
# Microsoft.Testing.Platform runner (preview) runs each test on its own to see what it covers, which the Api host's
# tests (hosts, PBKDF2, SQL Server) make take hours.
# Reports (html, markdown, json) go to StrykerOutput/<project>/; GitHub's job summary gets the markdown ones.
# Usage: eng/mutation.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
root="$PWD"
dotnet tool restore > /dev/null
rm -rf StrykerOutput

# The project to mutate, and the files of it to mutate.
targets=(
  "NovaCore.Abstractions.csproj|**/*.cs"
  "NovaCore.Modules.Catalog.csproj|**/Domain/**/*.cs"
  "NovaCore.Modules.Payments.csproj|**/Domain/**/*.cs"
)

for target in "${targets[@]}"; do
  project="${target%%|*}"
  files="${target#*|}"
  output="$root/StrykerOutput/${project%.csproj}"
  echo "== $project ($files)"
  (
    cd tests/NovaCore.Domain.Tests
    dotnet stryker --test-runner mtp --project "$project" --mutate "$files" --break-at 0 \
      --reporter cleartext --reporter html --reporter markdown --reporter json --output "$output"
  )
  if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
    { echo "## ${project%.csproj}"; cat "$output"/reports/mutation-report.md; echo; } >> "$GITHUB_STEP_SUMMARY"
  fi
done
