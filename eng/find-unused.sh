#!/usr/bin/env bash
# Unused code (docs/plan/DECISIONS.md D-066): Roslynator's find-symbol --unused lists the internal and private types,
# fields, constants, enum members and events nothing references. It stands in for CA1812, which the build doesn't run:
# CA1812 only looks for constructor calls, so it reports every class DI creates, while a type DI registers is referenced.
# Methods and properties are left out: Roslynator counts DI-called constructors, JSON-bound properties and minimal API
# handlers as unused (measured, R-119), and the build already reports unused private members (IDE0051). Public symbols
# are left out too: a package's public API is used by its consumers, not by this repository.
# Two entries are expected: Catalog's StockReservationConfiguration and Payments' PaymentConfiguration, which EF Core's
# ApplyConfigurationsFromAssembly finds by reflection (D-023 (5)).
# A report, not a gate: the list goes to stdout and, on GitHub, to the job summary.
# Usage: eng/find-unused.sh   (restore first)
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
dotnet tool restore > /dev/null

report=$(dotnet roslynator find-symbol NovaCore.slnx --unused --visibility internal private --ignore-generated-code \
  --symbol-kind type field const enum-field event --verbosity normal | grep -v -E '^(Loading|Analyze|  Analyze)')
echo "$report"

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  { echo '## Unused internal and private symbols'; echo; echo '```'; echo "$report"; echo '```'; } >> "$GITHUB_STEP_SUMMARY"
fi
