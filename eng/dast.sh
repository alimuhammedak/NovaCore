#!/usr/bin/env bash
# DAST against a running NovaCore host (docs/plan/PLAN.md F6e.2, DECISIONS D-044): Schemathesis (primary) fuzzes the
# OpenAPI document and eng/dast/protocol-openapi.yaml (/connect/* and /account/*, which the document leaves out); ZAP
# (secondary) runs its API scan (active) on the document and its baseline scan (passive, spidered) on the sign-in page.
# Both run from images pinned by digest, like eng/lint-workflows.sh (D-051).
#
# The host runs in Development (the OpenAPI document and the provider's ephemeral keys exist only there) with the DAST
# client of eng/dast/dast.env and its secret in DAST_CLIENT_SECRET; each scanner gets a fresh access token of that client
# (client credentials, 15 minutes), passed to its container through the environment, never on a command line.
#
# Usage: eng/dast.sh <report-dir>
# Environment:
#   DAST_CLIENT_SECRET   the DAST client's secret (required)
#   DAST_CA_FILE         the PEM certificate the host serves, trusted by curl and Schemathesis (required)
#   DAST_BASE_URL        the host as this machine reaches it (default https://localhost:5443)
#   DAST_CONTAINER_URL   the host as the scanners' containers reach it (default DAST_BASE_URL)
#   DAST_DOCKER_NETWORK  the containers' network (default host, Linux's way to reach localhost; on Docker Desktop
#                        bridge, with DAST_CONTAINER_URL=https://host.docker.internal:5443)
#   DAST_MAX_EXAMPLES    Schemathesis's examples per operation and phase (default: eng/dast/schemathesis.toml's)
#   DAST_SKIP_ZAP        1 skips ZAP (a quick local run)
# Exit code: 0 when every scan ran, whatever it found; the findings are in the reports and summed up in
# <report-dir>/summary.md. Non-zero when a scan couldn't run (no token, no document, a scanner's own error).
set -euo pipefail

schemathesis_image='schemathesis/schemathesis@sha256:ddf8b3c9e0b3ee7edfa65dad4d0c0e05c53fb5d92f99db379d40da01883474c7' # 4.33.1
zap_image='ghcr.io/zaproxy/zaproxy@sha256:9624669db72f54d86c4b46a7064518bd9bf79d8c61a4c31befa9923489f328af' # 2.17.0

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# Docker on Windows (Git Bash) wants a Windows path for a volume, and no MSYS path conversion of the arguments.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }
export MSYS_NO_PATHCONV=1

[[ $# -eq 1 ]] || { echo "usage: $0 <report-dir>" >&2; exit 2; }
: "${DAST_CLIENT_SECRET:?the secret of the DAST client}"
: "${DAST_CA_FILE:?the PEM certificate the host serves}"
base_url="${DAST_BASE_URL:-https://localhost:5443}"
container_url="${DAST_CONTAINER_URL:-$base_url}"
network="${DAST_DOCKER_NETWORK:-host}"

mkdir -p "$1"
reports="$(cd "$1" && pwd)"
cp "$DAST_CA_FILE" "$reports/host-certificate.pem"

# The client id, from the DAST configuration itself.
client_id="$(sed -n 's/^Identity__Clients__[0-9]*__ClientId=//p' "$root/eng/dast/dast.env")"
[[ -n "$client_id" ]] || { echo "eng/dast/dast.env names no client" >&2; exit 2; }

# A fresh access token of the DAST client into $access_token, masked in a GitHub Actions log. The secret goes to curl on
# stdin, exactly as given (--data-urlencode name@- keeps every byte), not on its command line.
access_token=''
fetch_token() {
  local response
  response="$(printf '%s' "$DAST_CLIENT_SECRET" | curl -fsS --cacert "$DAST_CA_FILE" \
    --data 'grant_type=client_credentials' --data-urlencode "client_id=$client_id" --data-urlencode 'client_secret@-' \
    "$base_url/connect/token")"
  access_token="$(jq -er '.access_token' <<< "$response")"
  if [[ -n "${GITHUB_ACTIONS:-}" ]]; then echo "::add-mask::$access_token"; fi
}

# The document as the host serves it: its servers entry is the address it was fetched from.
curl -fsS --cacert "$DAST_CA_FILE" -o "$(native "$reports/openapi.json")" "$base_url/openapi/v1.json"
# The containers see the host under DAST_CONTAINER_URL; the document's server must be that address.
jq --arg url "$container_url/" '.servers = [{ url: $url }]' "$(native "$reports/openapi.json")" > "$reports/openapi.target.json"
cp "$root/eng/dast/protocol-openapi.yaml" "$reports/protocol-openapi.yaml"
cp "$root/eng/dast/schemathesis.toml" "$reports/schemathesis.toml"

summary="$reports/summary.md"
printf '## DAST\n\n| Scan | Result |\n|---|---|\n' > "$summary"

# Schemathesis: 0 = every check passed, 1 = findings, anything else = it couldn't run.
schemathesis() {
  local name="$1" schema="$2" status=0
  fetch_token
  export DAST_TOKEN="$access_token"
  docker run --rm --network "$network" -e DAST_TOKEN \
    -v "$(native "$reports"):/dast" -w /dast "$schemathesis_image" \
    --config-file /dast/schemathesis.toml run "/dast/$schema" --url "$container_url" \
    --tls-verify /dast/host-certificate.pem \
    ${DAST_MAX_EXAMPLES:+--max-examples "$DAST_MAX_EXAMPLES"} \
    --report junit,html,har --report-dir "/dast/schemathesis-$name" || status=$?
  case "$status" in
    0) printf '| Schemathesis: %s | no finding |\n' "$name" >> "$summary" ;;
    1) printf '| Schemathesis: %s | **findings** (schemathesis-%s/) |\n' "$name" "$name" >> "$summary" ;;
    *) printf '| Schemathesis: %s | could not run (exit %s) |\n' "$name" "$status" >> "$summary"; return "$status" ;;
  esac
}

# ZAP's packaged scans: 0 = nothing, 1 = a FAIL, 2 = WARNs only, 3 = it couldn't run. It writes into /zap/wrk as its own
# user, so the directory must be writable to it.
zap() {
  local name="$1" status=0
  shift
  mkdir -p "$reports/zap-$name"
  chmod a+rwx "$reports/zap-$name"
  fetch_token
  export ZAP_AUTH_HEADER_VALUE="Bearer $access_token"
  docker run --rm --network "$network" -e ZAP_AUTH_HEADER_VALUE \
    -v "$(native "$reports/zap-$name"):/zap/wrk:rw" "$zap_image" \
    "$@" -r report.html -J report.json -x report.xml || status=$?
  case "$status" in
    0) printf '| ZAP: %s | no alert |\n' "$name" >> "$summary" ;;
    1|2) printf '| ZAP: %s | **alerts** (zap-%s/report.html) |\n' "$name" "$name" >> "$summary" ;;
    *) printf '| ZAP: %s | could not run (exit %s) |\n' "$name" "$status" >> "$summary"; return "$status" ;;
  esac
}

schemathesis api openapi.target.json
schemathesis protocol protocol-openapi.yaml

if [[ "${DAST_SKIP_ZAP:-}" != 1 ]]; then
  zap api zap-api-scan.py -t "$container_url/openapi/v1.json" -f openapi
  zap sign-in zap-baseline.py -t "$container_url/account/login" -m 1
fi

cat "$summary"
