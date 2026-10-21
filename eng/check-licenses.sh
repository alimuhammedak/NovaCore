#!/usr/bin/env bash
# License gate (docs/plan/DECISIONS.md Y-06, D-052): every package in the restored graph, transitive ones included, is
# MIT or Apache-2.0. Microsoft packages (Microsoft.*, System.*: prefixes reserved for Microsoft on nuget.org) pass
# whatever their license, as Y-06 allows; the packages they depend on are still checked. Polly.* is the one named
# exception: Microsoft.Extensions.Http.Resilience depends on it, under BSD-3-Clause (RESEARCH R-096).
# It reads the last restore's project.assets.json files, so restore first. Usage: eng/check-licenses.sh
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
dotnet tool restore > /dev/null
dotnet nuget-license --input NovaCore.slnx --include-transitive --allowed-license-types "MIT;Apache-2.0" \
  --ignored-packages "Microsoft.*;System.*;Polly.*" --error-only
