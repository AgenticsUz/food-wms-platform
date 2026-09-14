#!/usr/bin/env bash
# WMS backend testlarini yugurtiradi (platforma repo'sidagi skriptning aynan naqshi).
#
# NEGA `dotnet test` EMAS:
#   xunit.v3 test loyihasi o'z-o'zini yurituvchi EXECUTABLE (MTP v2 xosti).
#   .NET 10 SDK 10.0.301 ning `dotnet test` MTP rejimi u bilan hozircha to'g'ri
#   gaplashmaydi ("0 test", exit 5) — ya'ni to'plam umuman yurmasa ham YASHIL
#   ko'rinardi. Shuning uchun DLL to'g'ridan-to'g'ri `dotnet exec` bilan yuriladi.
#
# IKKI DARVOZA («soxta yashil»ga qarshi):
#   1) DLL topilmasa — XATO (SKIP emas).
#   2) To'plam yugurdi, lekin test soni 0 — XATO.
#
# ⚠️ Testlar HAQIQIY Postgres konteynerini ko'taradi (Testcontainers) — Docker
#    ishlab turishi SHART. Docker yo'q bo'lsa to'plam qulaydi, jimgina o'tmaydi.
#
# Ishlatilishi:
#   bash scripts/run-tests.sh                 # Release
#   bash scripts/run-tests.sh Debug
#   WMS_SKIP_BUILD=1   — build qadamini o'tkazib yuboradi
#   WMS_TEST_PROJECTS="tests/WMS.Tests" — ro'yxatni almashtiradi
set -uo pipefail

total_tests_from_output() {
  local numbers
  numbers="$(printf '%s\n' "$1" | grep -oE 'Total:[[:space:]]*[0-9]+' | grep -oE '[0-9]+$')"
  [[ -z "$numbers" ]] && return 0
  printf '%s\n' "$numbers" | awk '{ sum += $1 } END { print sum + 0 }'
}

CONFIG="${1:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ -n "${WMS_TEST_PROJECTS:-}" ]]; then
  # shellcheck disable=SC2206
  PROJECTS=(${WMS_TEST_PROJECTS})
else
  PROJECTS=("tests/WMS.Tests")
fi

if [[ "${WMS_SKIP_BUILD:-0}" == "1" ]]; then
  echo "== Build o'tkazib yuborildi (WMS_SKIP_BUILD=1) =="
else
  echo "== Build ($CONFIG) =="
  dotnet build AgenticsWms.slnx -c "$CONFIG" -nodeReuse:false --nologo || exit 1
fi

FAILED=0
GRAND_TOTAL=0
for proj in "${PROJECTS[@]}"; do
  name="$(basename "$proj")"
  dll="$proj/bin/$CONFIG/net10.0/$name.dll"
  echo
  echo "== $name =="
  if [[ ! -f "$dll" ]]; then
    echo "  XATO: $dll topilmadi — test to'plami umuman yugurmadi."
    FAILED=1
    continue
  fi

  output="$(dotnet exec "$dll" "${@:2}" 2>&1)"
  status=$?
  printf '%s\n' "$output"
  [[ $status -ne 0 ]] && FAILED=1

  total="$(total_tests_from_output "$output")"
  if [[ -z "$total" ]]; then
    echo "  XATO: $name chiqishida 'Total: N' xulosasi yo'q."
    FAILED=1
  elif [[ "$total" -eq 0 ]]; then
    echo "  XATO: $name da 0 ta test bajarildi — to'plam bo'sh (soxta yashil)."
    FAILED=1
  else
    echo "  Bajarilgan test soni: $total"
    GRAND_TOTAL=$((GRAND_TOTAL + total))
  fi
done

echo
echo "JAMI bajarilgan test: $GRAND_TOTAL"
if [[ $FAILED -eq 0 ]]; then
  echo "NATIJA: barcha backend testlari o'tdi."
else
  echo "NATIJA: XATO — kamida bitta test to'plami quladi yoki umuman yugurmadi."
fi
exit $FAILED
