#!/usr/bin/env bash
# Checks a running compact deployment end to end through the gateway: screens, micro-frontend, Keycloak, API + PostgreSQL.
# Usage: deploy/smoke-test.sh [base-url]        (default http://localhost:8080)
# Writes one test cheque (bank "SMOKE-TEST"). Use on a test server only, never on a client's live data.
set -euo pipefail
base="${1:-http://localhost:8080}"
fail() { echo "FAIL: $*" >&2; exit 1; }
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }

# Wait until the API answers (containers may still be starting).
for _ in $(seq 1 60); do
  [[ "$(status "$base/api/cashflow/instruments/00000000-0000-0000-0000-000000000000")" == 404 ]] && break
  sleep 2
done

[[ "$(status "$base/")" == 200 ]] || fail "shell does not load"
location="$(curl -s -o /dev/null -w '%{redirect_url}' "$base/receipts")"
[[ "$location" == "$base/receipts/" ]] || fail "/receipts redirects to '$location' instead of '$base/receipts/'"
[[ "$(status "$base/remotes/receipts/mf-manifest.json")" == 200 ]] || fail "receipts micro-frontend manifest missing"
[[ "$(status "$base/remotes/receipts/remoteEntry.js")" == 200 ]] || fail "receipts micro-frontend entry missing"

# Keycloak must advertise the address users actually use (host and port), or logins and token checks fail.
for _ in $(seq 1 60); do
  issuer="$(curl -s "$base/auth/realms/master/.well-known/openid-configuration" | grep -o '"issuer":"[^"]*"' || true)"
  [[ -n "$issuer" ]] && break
  sleep 2
done
[[ "$issuer" == "\"issuer\":\"$base/auth/realms/master\"" ]] || fail "Keycloak issuer is $issuer, expected $base/auth/realms/master"

# Receive a cheque, read it back, deposit it (API -> PostgreSQL as cashflow_svc).
key="smoke-$(date +%s)-$RANDOM"
body='{"kind":"OrdinaryCheque","number":"'"$RANDOM"'","bankName":"SMOKE-TEST","amount":{"amount":1000000},"receivedOn":"1405/08/19","dueDate":"1405/09/30","drawerId":"5b1d3c6e-2f43-4a51-9a5b-0c3e1d2f4a11","drawerName":"Smoke test","purpose":"Smoke test"}'
created="$(curl -s -D - -o /dev/null -X POST "$base/api/cashflow/instruments/received" -H 'Content-Type: application/json' -H "Idempotency-Key: $key" -d "$body")"
grep -q '^HTTP/[0-9.]* 201' <<<"$created" || fail "receiving a cheque: $(head -1 <<<"$created")"
path="$(grep -i '^location:' <<<"$created" | tr -d '\r' | awk '{print $2}')"
curl -sf "$base$path" | grep -q '"state":"InCashDesk"' || fail "reading the cheque back"
[[ "$(status -X POST "$base$path/actions/DepositForCollection" -H 'Content-Type: application/json' -H "Idempotency-Key: $key-deposit" -d '{"date":"1405/08/20","destination":"Smoke account"}')" == 201 ]] || fail "depositing the cheque"
curl -sf "$base$path" | grep -q '"state":"DepositedForCollection"' || fail "state after deposit"

echo "OK: shell, receipts micro-frontend, Keycloak and the cashflow API with PostgreSQL work at $base"
