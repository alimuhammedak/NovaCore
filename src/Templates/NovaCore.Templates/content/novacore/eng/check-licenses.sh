#!/usr/bin/env bash
# License gate: every package in the restored graph, transitive ones included, is MIT or Apache-2.0 licensed. Microsoft
# packages (Microsoft.*, System.*: prefixes nuget.org reserves for Microsoft) pass whatever their license; the packages
# they bring are still checked. Every other license is a named exception, a decision of this project:
# - Polly.*, only as a transitive dependency: Microsoft.Extensions.Http.Resilience, under NovaCore's AddCoreDefaults,
#   depends on it, under BSD-3-Clause. A project that references Polly itself fails the first check.
# Name an exception by its exact package ID, not a pattern: a name matches only itself, so another package of the same
# family with another license still fails.
# It reads the restore's project.assets.json files, so restore first. Usage: bash eng/check-licenses.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
dotnet tool restore

echo "== Packages the projects reference directly"
dotnet nuget-license --input MyService.slnx --allowed-license-types "MIT;Apache-2.0" \
  --ignored-packages "Microsoft.*;System.*" --error-only

echo "== Every package, transitive ones included"
dotnet nuget-license --input MyService.slnx --include-transitive --allowed-license-types "MIT;Apache-2.0" \
  --ignored-packages "Microsoft.*;System.*;Polly.*" --error-only
