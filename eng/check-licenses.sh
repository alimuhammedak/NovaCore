#!/usr/bin/env bash
# License gate (docs/plan/DECISIONS.md Y-47, D-052, D-068): every package in the restored graph, transitive ones included,
# is MIT or Apache-2.0. Microsoft packages (Microsoft.*, System.*: prefixes reserved for Microsoft on nuget.org) pass
# whatever their license, as Y-47 allows; the packages they depend on are still checked. The named exceptions:
# - Polly.*, only as a transitive dependency: Microsoft.Extensions.Http.Resilience depends on it, under BSD-3-Clause
#   (RESEARCH R-096). A project that references Polly itself fails the first check.
# - The database provider and its driver, by their exact package IDs (D-068): here the PostgreSQL test layer's, under the
#   PostgreSQL license. A name matches only itself, so another package of the family (Npgsql.NodaTime) still fails.
# It reads the last restore's project.assets.json files, so restore first. Usage: eng/check-licenses.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
dotnet tool restore

providers="Npgsql;Npgsql.EntityFrameworkCore.PostgreSQL"

echo "== Packages the projects reference directly"
dotnet nuget-license --input NovaCore.slnx --allowed-license-types "MIT;Apache-2.0" \
  --ignored-packages "Microsoft.*;System.*;$providers" --error-only

echo "== Every package, transitive ones included"
dotnet nuget-license --input NovaCore.slnx --include-transitive --allowed-license-types "MIT;Apache-2.0" \
  --ignored-packages "Microsoft.*;System.*;$providers;Polly.*" --error-only
