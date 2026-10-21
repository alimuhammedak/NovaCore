#!/usr/bin/env bash
# License gate (docs/plan/DECISIONS.md Y-06, D-052): every package in the restored graph, transitive ones included, is
# MIT or Apache-2.0. Microsoft packages (Microsoft.*, System.*: prefixes reserved for Microsoft on nuget.org) pass
# whatever their license, as Y-06 allows; the packages they depend on are still checked. Polly.* is the one named
# exception, and only as a transitive dependency: Microsoft.Extensions.Http.Resilience depends on it, under BSD-3-Clause
# (RESEARCH R-096). A project that references Polly itself fails the first check.
# It reads the last restore's project.assets.json files, so restore first. Usage: eng/check-licenses.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
dotnet tool restore

echo "== Packages the projects reference directly"
dotnet nuget-license --input NovaCore.slnx --allowed-license-types "MIT;Apache-2.0" \
  --ignored-packages "Microsoft.*;System.*" --error-only

echo "== Every package, transitive ones included"
dotnet nuget-license --input NovaCore.slnx --include-transitive --allowed-license-types "MIT;Apache-2.0" \
  --ignored-packages "Microsoft.*;System.*;Polly.*" --error-only
