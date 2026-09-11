#!/bin/sh
# =============================================================================
#  AGENTICS WMS — runtime konfiguratsiyani yasash (web konteyner)
#
#  Image ichida `assets/config/app-config.json` YO'Q (Dockerfile.web uni o'chiradi).
#  Bu skript nginx ishga tushishidan oldin (/docker-entrypoint.d/) uni muhit
#  o'zgaruvchilaridan yozadi — shu sabab BITTA image barcha stendlarga yaraydi
#  va manzil o'zgargani uchun qayta build qilish shart emas. Yo'l
#  `@agentics/config` ning `CONFIG_URL` sukutiga mos (`assets/config/app-config.json`).
#
#  Kutiladigan o'zgaruvchilar:
#     PUBLIC_API_URL        (default `/api`  — bir originli deploy)
#     PUBLIC_IDENTITY_URL   (MAJBURIY: OIDC provider bazasi, masalan https://id.agentics.uz)
#     DEFAULT_LOCALE        (default `uz-Latn`)
#     IDLE_TIMEOUT_MINUTES  (default 30)
# =============================================================================
set -eu

API_URL="${PUBLIC_API_URL:-/api}"
LOCALE="${DEFAULT_LOCALE:-uz-Latn}"
IDLE_MINUTES="${IDLE_TIMEOUT_MINUTES:-30}"

# Identity manzili bo'lmasa TO'XTAYMIZ. Zaxira qiymat qo'yish xavfli: ilova
# jimgina noto'g'ri (yoki mavjud bo'lmagan) provider'ga borar edi va buni
# faqat foydalanuvchi login paytida sezardi (fail-closed).
if [ -z "${PUBLIC_IDENTITY_URL:-}" ]; then
    echo "[app-config] XATO: PUBLIC_IDENTITY_URL berilmagan — konteyner to'xtatildi." >&2
    exit 1
fi

# Qiymatlarni QOCHIRMAYMIZ, TEKSHIRAMIZ. Bular manzil va til kodi — ularda
# qo'shtirnoq yoki teskari chiziq bo'lishi mumkin emas. Qochirish o'rniga rad
# etish JSON'ni ham, sababni ham aniq qoldiradi (jimgina buzuq fayl chiqmaydi).
require_plain() {
    name="$1"
    value="$2"
    case "$value" in
        *'"'* | *'\'*)
            echo "[app-config] XATO: $name da ruxsat etilmagan belgi (\" yoki \\)." >&2
            exit 1
            ;;
    esac

    # Yangi qator ham JSON'ni buzadi. `case` shabloni bilan tekshirib bo'lmaydi:
    # `$(printf '\n')` buyruq almashinuvida oxirgi qator uzilib, shablon bo'sh
    # qolar va HAR QANDAY qiymatga mos kelib qolardi.
    if [ "$value" != "$(printf '%s' "$value" | tr -d '\n\r')" ]; then
        echo "[app-config] XATO: $name da yangi qator belgisi bor." >&2
        exit 1
    fi
}

require_plain PUBLIC_API_URL "$API_URL"
require_plain PUBLIC_IDENTITY_URL "$PUBLIC_IDENTITY_URL"
require_plain DEFAULT_LOCALE "$LOCALE"

# Butun son bo'lmasa JSON sintaktik buzuq bo'lardi (qo'shtirnoqsiz maydon).
case "$IDLE_MINUTES" in
    '' | *[!0-9]*)
        echo "[app-config] XATO: IDLE_TIMEOUT_MINUTES butun son bo'lishi kerak." >&2
        exit 1
        ;;
esac

write_config() {
    dir="$1"
    client_id="$2"
    scope="$3"
    audience="$4"

    mkdir -p "$dir"
    cat > "$dir/app-config.json" <<JSON
{
  "apiUrl": "${API_URL}",
  "identityUrl": "${PUBLIC_IDENTITY_URL}",
  "clientId": "${client_id}",
  "scope": "${scope}",
  "audience": "${audience}",
  "defaultLocale": "${LOCALE}",
  "idleTimeoutMinutes": ${IDLE_MINUTES},
  "features": {}
}
JSON
    echo "[app-config] ${dir}/app-config.json yozildi (client=${client_id})"
}

# Bitta chaqiruv — bitta SPA. WMS'ning admin ekranlari Console'da (`wms.admin`
# scope'i `console` mijoziniki), shuning uchun bu yerda ikkinchi mijoz yo'q.
#
# ⚠️ `signalRUrl` ATAYLAB yozilmaydi: WMS'da hub yo'q. `@agentics/config` maydon
# berilmasa sukutni (`/hubs`) qo'yadi va hech kim uni ishlatmaydi — yozilsa esa
# mavjud bo'lmagan yo'lni «bor» deb ko'rsatardi.
# ⚠️ `phone` — JIT profil nusxasi (`user_profile.phone`, D5) telefonni TOKENDAN
# oladi; `phone` scope'isiz u access tokenga UMUMAN tushmaydi (Wash F4 da o'lchangan).
# ⚠️ F8.1: `identity.tenant` — «Kirish hisoblari» sahifasi Identity'ning `/tenant/v1`
# yuzasini SHU token bilan chaqiradi; scope so'ralmasa token `identity-tenant-api`
# audience'isiz chiqadi va sahifa 403 oladi (F3 darsi, HOLAT §5.6).
write_config /usr/share/nginx/html/assets/config wms-web \
    "openid profile phone offline_access wms.api identity.tenant" wms-api
