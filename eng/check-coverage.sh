#!/usr/bin/env bash
# Coverage ratchet (docs/plan/DECISIONS.md D-060): runs the tests with coverage (coverlet.MTP, every NovaCore.* assembly
# but the tests and the migrations) and fails when the line or the branch coverage falls below eng/coverage-baseline.txt
# by more than the run-to-run noise (TOLERANCE). The baseline is what main reached; a change that raises the coverage can
# write the new values with --update and commit the file with it. Lowering it is an edit of that file, visible in the diff.
#
# The architecture tests run without coverage (a separate `dotnet test`): coverlet instruments the built assemblies in
# place, and those tests inspect the built assemblies' references. The SQL Server tests are part of the baseline, so
# without Docker (they skip) the run fails (--fail-skips on).
#
# Usage: eng/check-coverage.sh [configuration] [--update]   (build first; the configuration defaults to Release)
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

configuration=Release
update=false
for argument in "$@"; do
  case "$argument" in
    --update) update=true ;;
    *) configuration="$argument" ;;
  esac
done

TOLERANCE=0.3
results=TestResults/coverage
baseline=eng/coverage-baseline.txt
rm -rf "$results"

dotnet test --solution NovaCore.slnx -c "$configuration" --no-build --results-directory "$results" -- \
  --fail-skips on --filter-not-namespace NovaCore.Api.Tests.Architecture \
  --coverlet --coverlet-output-format cobertura \
  --coverlet-include "[NovaCore.*]*" --coverlet-exclude "[NovaCore.Api.Tests]*" \
  --coverlet-exclude-by-file "**/Migrations/*.cs"

dotnet tool restore > /dev/null
dotnet reportgenerator "-reports:$results/coverage.cobertura.*.xml" "-targetdir:$results/report" \
  "-reporttypes:TextSummary;MarkdownSummaryGithub" > /dev/null

# "Line coverage: 95.2%" in the text summary, "line=95.2" in the baseline. No jq: it isn't on every machine this runs on.
summary() { sed -n "s/^ *$1 coverage: \([0-9.]*\)%.*/\1/p" "$results/report/Summary.txt"; }
recorded() { sed -n "s/^$1=//p" "$baseline"; }
line=$(summary Line)
branch=$(summary Branch)
baseline_line=$(recorded line)
baseline_branch=$(recorded branch)

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  cat "$results/report/SummaryGithub.md" >> "$GITHUB_STEP_SUMMARY"
fi

echo "== Line coverage $line% (baseline $baseline_line%), branch coverage $branch% (baseline $baseline_branch%)"

if [[ "$update" == true ]]; then
  # Only upward, a tenth of a point at a time.
  raise() { awk -v current="$1" -v old="$2" 'BEGIN { v = int(current * 10) / 10; print (v > old ? v : old) }'; }
  new_line=$(raise "$line" "$baseline_line")
  new_branch=$(raise "$branch" "$baseline_branch")
  printf '# The coverage main reached, in percent (eng/check-coverage.sh).\nline=%s\nbranch=%s\n' \
    "$new_line" "$new_branch" > "$baseline"
  echo "== $baseline: line $new_line%, branch $new_branch%"
  exit 0
fi

failed=$(awk -v line="$line" -v branch="$branch" -v base_line="$baseline_line" -v base_branch="$baseline_branch" \
  -v tolerance="$TOLERANCE" 'BEGIN { print (line < base_line - tolerance || branch < base_branch - tolerance) ? 1 : 0 }')
if [[ "$failed" == 1 ]]; then
  echo "::error::Coverage fell below the baseline in $baseline by more than $TOLERANCE points."
  exit 1
fi
