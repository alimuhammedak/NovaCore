#!/usr/bin/env bash
# Consumer smoke test of the template (docs/plan/PLAN.md F5.2). Packs the NovaCore packages and the template into a local
# feed, creates a project from the template with each identity option, and builds and tests it the way a consumer does:
# the packages come from the feed (not from this repository's projects), warnings are errors, and the migrations must
# match the model. Usage: eng/template-smoke.sh [work-directory]
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ -n "${1:-}" ]]; then
  work="$1"
else
  work="$(mktemp -d)"
  trap 'rm -rf "$work"' EXIT
fi
rm -rf "$work/feed" "$work/hive" "$work/packages" "$work/projects"
mkdir -p "$work/projects"

# dotnet on Windows (Git Bash) wants Windows paths in options and environment variables.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }

feed="$(native "$work/feed")"
hive="$(native "$work/hive")"
# A package cache of its own: the packed packages reuse one version, and a shared cache would keep a stale copy.
export NUGET_PACKAGES="$(native "$work/packages")"

echo "== Packing into $feed"
dotnet pack "$root/NovaCore.slnx" -c Release -o "$feed" --version-suffix smoke

template="$(ls "$work"/feed/BMD.NovaCore.Templates.*.nupkg)"
echo "== Installing $(basename "$template") into a template hive of its own"
dotnet new install "$(native "$template")" --debug:custom-hive "$hive"

for auth in BuiltIn External; do
  name="Smoke$auth"
  project="$work/projects/$name"
  echo "== dotnet new novacore --auth $auth"
  dotnet new novacore -n "$name" --auth "$auth" -o "$(native "$project")" --debug:custom-hive "$hive"

  # The generated nuget.config maps BMD.NovaCore.* to the GitHub feed; here the same source name points at the local one.
  (cd "$project" && dotnet nuget update source novacore --source "$feed" --configfile nuget.config)

  echo "== $name: build, test, package licenses"
  (cd "$project" && dotnet build -c Release && dotnet test -c Release --no-build && bash eng/check-licenses.sh)

  echo "== $name: migrations match the model"
  (
    cd "$project"
    dotnet tool restore
    api="src/Api/$name.Api"
    dotnet ef migrations has-pending-model-changes --project "$api" --startup-project "$api" \
      --context TodosDbContext --configuration Release --no-build
    if [[ "$auth" == BuiltIn ]]; then
      dotnet ef migrations has-pending-model-changes --project "$api" --startup-project "$api" \
        --context IdentityServerDbContext --configuration Release --no-build
    fi
  )

  echo "== $name: nothing of this repository's own"
  for path in docs .claude .template.config PACKAGE.md .github/workflows/release.yml; do
    if [[ -e "$project/$path" ]]; then echo "::error::$name contains $path"; exit 1; fi
  done
  if find "$project" -name 'PublicAPI.*.txt' -not -path '*/bin/*' -not -path '*/obj/*' | grep -q .; then
    echo "::error::$name contains PublicAPI files"; exit 1
  fi
  if grep -rIl --exclude-dir=bin --exclude-dir=obj -e 'Muhammed' -e 'NOVACORE_PACKAGE_VERSION' -e 'NOVACORE_HTTPS_PORT' -e 'NOVACORE_HTTP_PORT' -e 'MyService' "$project"; then
    echo "::error::$name contains the author's name or an unreplaced template token (files above)"; exit 1
  fi
done

echo "== Template smoke test passed"
