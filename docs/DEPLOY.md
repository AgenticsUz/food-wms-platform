# DEPLOY — WMS'ni prodga chiqarish (F7 usuli, SSH orqali)

Bu WMS uchun yagona deploy tartibi. 2026-09-11 dagi F7 deploy'ida ishlatilgan
(`agentics-platform/docs/DEPLOY-SERVER.md`, `_deploy-bundle/PROD-HISOBLAR.md`).

## 0. Qoidalar

1. **Image serverda QURILMAYDI.** VPS 2 vCPU / 4 GB — Angular yoki .NET build'i
   ishlab turgan stack'larni swap'ga suradi. Lokalda quriladi, `docker save` bilan
   ko'chiriladi, serverda faqat `docker load` + `up --no-build`.
2. GitHub `deploy.yml` workflow'i o'chiq (`DEPLOY_ENABLED` yo'q) — undan foydalanilmaydi.
3. Deploy'dan oldin lokalda `lint,test,build` yashil va commit push qilingan bo'lsin.
4. Faqat o'zgargan servis qayta ko'tariladi (`--no-deps`); postgres'ga tegilmaydi.
5. Eski image har safar `pre-<sana-vaqt>` tegi bilan zaxiralanadi — orqaga qaytish shu teg.
6. Serverdagi `/opt/agentics/wms` — `git archive` nusxasi (git EMAS). Image deploy'i
   uni yangilamaydi; compose yoki nginx fayli o'zgarganda alohida ko'chiriladi (§4).

## 1. Server

| | |
|---|---|
| Host | `45.138.158.200` (`vps02473.eskiz.uz`) |
| SSH | `ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200` |
| Stack | `/opt/agentics/wms/docker`, compose `docker-compose.prod.yml`, `.env` serverda |
| Image nomlari | `.env`: `IMAGE_REGISTRY=agentics-wms`, `IMAGE_TAG=local` → `agentics-wms/wms-web:local`, `agentics-wms/wms-api:local` |
| Port | `wms-web` → `127.0.0.1:6510`, host nginx → `https://wms.agentics.uz` |

Barcha buyruqlar `wms` repo ildizidan. Platforma paketlari
(`../agentics-platform/artifacts/{npm,nuget}`) mavjud bo'lishi shart.

## 2. Frontend (`wms-web`)

```bash
# 1) Image lokalda
docker build -f docker/Dockerfile.web \
  --build-context platform=../agentics-platform/artifacts \
  -t agentics-wms/wms-web:local .

# 2) Ichida yangi kod borligini tekshirish (biror yangi class/kalit bo'yicha)
docker run --rm --entrypoint sh agentics-wms/wms-web:local \
  -c 'grep -l "<yangi-belgi>" /usr/share/nginx/html/*.js | head -1'

# 3) Tarball → server
docker save agentics-wms/wms-web:local | gzip -1 > ../_deploy-bundle/wms-web.tar.gz
scp -i ~/.ssh/agentics-demo ../_deploy-bundle/wms-web.tar.gz deploy@45.138.158.200:/tmp/

# 4) Serverda: zaxira teg → load → faqat wms-web
ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200 'set -e
  TAG=pre-$(date +%Y%m%d-%H%M)
  docker tag agentics-wms/wms-web:local agentics-wms/wms-web:$TAG && echo "backup: $TAG"
  gunzip -c /tmp/wms-web.tar.gz | docker load
  cd /opt/agentics/wms/docker
  docker compose -f docker-compose.prod.yml up -d --no-build --no-deps wms-web
  rm -f /tmp/wms-web.tar.gz
  sleep 8; docker ps --filter name=wms-web --format "{{.Names}} {{.Status}}"
  curl -s -o /dev/null -w "https login -> %{http_code}\n" https://wms.agentics.uz/login'
```

## 3. Backend (`wms-api`)

Migratsiya bor bo'lsa AVVAL `pg_dump` (§5) — migratsiya orqaga qaytmaydi.

```bash
docker build -f docker/Dockerfile.api \
  --build-context platform=../agentics-platform/artifacts \
  --build-arg PROJECT_PATH=src/WMS.API/WMS.API.csproj --build-arg ASSEMBLY_NAME=WMS.API \
  -t agentics-wms/wms-api:local .

docker save agentics-wms/wms-api:local | gzip -1 > ../_deploy-bundle/wms-api.tar.gz
scp -i ~/.ssh/agentics-demo ../_deploy-bundle/wms-api.tar.gz deploy@45.138.158.200:/tmp/

# wms-migrator ham shu image'da: compose zanjirida avval migrator, keyin api ko'tariladi.
ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200 'set -e
  TAG=pre-$(date +%Y%m%d-%H%M)
  docker tag agentics-wms/wms-api:local agentics-wms/wms-api:$TAG && echo "backup: $TAG"
  gunzip -c /tmp/wms-api.tar.gz | docker load
  cd /opt/agentics/wms/docker
  docker compose -f docker-compose.prod.yml up -d --no-build wms-migrator wms-api
  rm -f /tmp/wms-api.tar.gz
  sleep 10; docker ps --filter name=wms- --format "{{.Names}} {{.Status}}"
  curl -s -o /dev/null -w "health -> %{http_code}\n" http://127.0.0.1:6510/api/health'
```

## 4. Compose / nginx / init SQL o'zgarganda

```bash
git archive --format=tar HEAD docker/docker-compose.prod.yml docker/postgres/init docker/host-nginx \
  | ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200 'cd /opt/agentics/wms && tar -xf -'
# keyin tegishli servisni up -d --no-build; host nginx o'zgarsa —
# agentics-platform/deploy/host-nginx/render.sh + nginx reload (DEPLOY-SERVER.md §5)
```

## 5. Zaxira va orqaga qaytish

```bash
# Baza (migratsiyali deploy'dan oldin)
ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200 \
  'docker exec wms-postgres pg_dump -U postgres -Fc agentics_wms > /opt/agentics/backups/wms-$(date +%Y%m%d-%H%M).dump'

# Image'ni qaytarish (web misolida)
ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200 'set -e
  docker tag agentics-wms/wms-web:pre-YYYYMMDD-HHMM agentics-wms/wms-web:local
  cd /opt/agentics/wms/docker && docker compose -f docker-compose.prod.yml up -d --no-build --no-deps wms-web'
```

## 5.1 Saboqlar (deploy amalda topgan)

📌 **Migratsiya tenant jadvaliga MA'LUMOT yoza olmaydi** (2026-09-14). `wms.*`
tenant jadvallarida RLS `FORCE` rejimida, migratsiya esa `app_migrator` roli bilan
ketadi (`rolbypassrls = false`) — tenant konteksti qo'yilmagan `UPDATE`/`INSERT`
**0 qatorga tegadi va XATO BERMAYDI**. `AddIdentityRoleToProfile` dagi backfill
aynan shunday jim yo'qolgan. Ma'lumotni to'ldirish kerak bo'lsa — servis qatlamida
(tenant konteksti bilan) yoki JIT/fon vazifada qiling, migratsiyada emas.

📌 **`NO FORCE` ro'yxati JOIN qilinadigan jadvallarni ham o'z ichiga oladi**
(2026-09-15). Migratsiya backfill'i RLS'ni chetlab o'tish uchun jadvalni vaqtincha
`NO FORCE ROW LEVEL SECURITY` qiladi. ⚠️ `FORCE` faqat YOZISHGA emas, **O'QISHGA ham**
qo'llanadi: `F10_DocumentsAndCosts` da `batch.unit_cost` ni to'ldiradigan `UPDATE`
`transfer_item` dan o'qirdi, o'sha jadval esa ro'yxatda yo'q edi — natijada gap XATO
BERMASDAN 0 qatorga tegdi va tannarx bo'sh qoldi. Lokal stendda (ma'lumotli bazada)
o'lchab topildi; bo'sh test bazasida ko'rinmasdi. Qoida: backfill SQL'idagi HAR jadval —
yoziladigani ham, o'qiladigani ham — `NO FORCE` ro'yxatida bo'lsin, va migratsiya
ma'lumoti bor bazada sinab ko'rilsin.

📌 **Kabinet (F9) prod'da Identity sozlamasini talab qiladi:** `identity.product`
da `wms` rollari ro'yxatida `client` va `agent` bo'lishi shart (Console →
Mahsulotlar), aks holda «Kabinet ochish» `IDN-TEN-003` beradi.

## 6. Tekshiruv

- `docker ps` — konteyner `(healthy)`.
- Serverdan `curl https://wms.agentics.uz/login` → 200 (lokal mashinadan curl 000 qaytarishi
  mumkin — tarmoq, xato emas; serverdan tekshiring).
- Yangi bundle: `curl -s https://wms.agentics.uz/<chunk>.js | grep -c "<yangi-belgi>"` → 1.
- Brauzerda sahifani ochib ko'rish.
