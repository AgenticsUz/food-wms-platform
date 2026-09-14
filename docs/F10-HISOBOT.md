# F10 — bosqich hisobotlari

> Har bosqich (P → A → K) yakunida shu faylga qisqa hisobot yoziladi: nima
> qilindi, qabul mezoni natijasi, ochiq qolganlar. Reja — `docs/AI/WMS-AI-REJA.md`,
> ish varag'i — `docs/AI/P-CHEKLIST.md`.

---

## P0 — ochiq bandlarni yopish (2026-09-14)

### §3 SQL tekshiruvi — `01a0964f-…` hujjatining sababi ANIQLANDI

Prod (`45.138.158.200`, `agentics_wms`) da faqat o'qish so'rovlari:

| So'rov | Natija |
|---|---|
| Hujjat va qatorlari | `type=2` (Outgoing), `status=1` (Pending), manba ombor `01a0964c-…` (**Sklad1**), mahsulot `01a09648-…` (**Snikers**), miqdor **34** |
| O'sha mahsulot bo'yicha qoldiq qatorlari | **0 qator** |
| Butun bazadagi qoldiq qatorlari | 14 ta, ulardan **o'chirilgan partiyali — 0 ta** |
| Omborlar kesimi | Xom ashyo ombori 8, Tayyor mahsulot ombori 6, **Sklad1 — 0** |

**Xulosa.** Ikkala gumon ham tasdiqlanmadi: qoldiq boshqa omborda ham turmagan,
yumshoq o'chirilgan partiya bazada umuman yo'q. «Snikers» mahsulotiga hech qachon
kirim qilinmagan — ya'ni tasdiqdagi «omborda yetarli qoldiq yo'q» xatosi
MAZMUNAN TO'G'RI edi. Haqiqiy nuqson faqat foydalanuvchi tajribasida: xato
hujjat yaratilganidan keyin, tasdiq bosqichida chiqqan va qancha yetmaganini
aytmagan — ikkalasi ham T2 (`096f2be`) da tuzatilgan
(`EnsureStockAvailableAsync` + «kerak N, mavjud M» xabari).

### O'chirilgan partiya masalasi (XATOLAR §3, 4-ish) — qaror va tuzatish

Prod'da bunday qator yo'q, lekin nuqson LATENT holda turardi: `StockAllocator`
partiyaga `EXISTS` qilardi (yumshoq o'chirish filtri bilan), qoldiq ekrani
(`WarehouseService.GetStockAsync`) esa partiyaga umuman tegmaydi — «ekranda bor,
chiqimga chiqmaydi» holati.

**Qaror:** haqiqat — qoldiq qatorining o'zi; partiya faqat FEFO tartibi uchun
metama'lumot va uni o'chirish tovarni omborda yo'q qilmaydi. `AvailableRows`
endi yumshoq-o'chirish filtrini o'chirib, qoldiq qatorining O'Z `is_deleted` ini
tekshiradi. Darvoza testlari — `tests/WMS.Tests/Catalog/StockAllocatorTests.cs`.

---

## P1 — backend test poydevori (2026-09-14)

`tests/WMS.Tests` — xUnit v3 + Testcontainers PostgreSQL 17, InMemory EMAS:
tekshiriladigan da'volar bazaning o'ziga tayanadi (RLS siyosati, `xmin`,
filtrli noyob indeks, `numeric(18,3)`).

- Fixture konteynerni ko'taradi, init skriptining test nusxasini yuritadi
  (kengaytmalar + `app_user` **NOBYPASSRLS** / `app_migrator` rollari), haqiqiy
  migratsiyani (`WmsDatabaseMigrator`, RLS generatori bilan) qo'llaydi va TO'LIQ
  DI grafini quradi (`AddWmsInfrastructure` + `AddWmsModules`).
- Har test O'Z tenantini oladi — testlar orasida `TRUNCATE` yo'q, ajratishni RLS
  qiladi (bu ham o'lchanadi).
- `scripts/run-tests.sh` — platforma naqshi, ikki darvoza: DLL yo'q → xato,
  `Total: 0` → xato.

**Qamrov (44 test, hammasi yashil):**

| To'plam | Nima o'lchanadi |
|---|---|
| `Infrastructure/RlsIsolationTests` (2) | Boshqa tenant qatorlari ko'rinmaydi; kontekstsiz qamrov 0 qator (global filtr O'CHIRIB tekshiriladi — himoya RLS'da) |
| `Infrastructure/RlsPolicyTests` (6) | `app_user` NOBYPASSRLS va superuser emas; 5 jadvalda `tenant_isolation` + `FORCE` |
| `Trade/TransferServiceTests` (6) | FEFO tartibi, «kerak N, mavjud M» YARATISHDA, kirimda partiya, parallel 409, rad etish, ichki ko'chirish |
| `Trade/TransferCommissionTests` (3) | Komissiya summasi/yaxlitlash, ikkinchi tasdiq, noyob indeks |
| `Trade/TransferPermissionGateTests` (6) | Rol to'plamlari va `[RequirePermission]` atributlari (refleksiya) |
| `Catalog/StockAllocatorTests` (6) | «Qancha bor» = «yechib ber»; o'chirilgan partiya/qator; muddatsiz partiya oxirida |
| `Catalog/SearchServiceTests` (6) | Uch alifboda bir mahsulot; 3 nomzod; tenant ajratilishi |
| `Catalog/ProductSearchPathTests` (3) | UI yuradigan yo'l: `GetAllAsync(search:)`, `name_search` yaratish/tahrirda yoziladi |

**Darvozalar HAQIQATAN qizil beradimi** (qabul mezoni). Vaqtinchalik `git worktree`
da eski kodga qarshi yurgizildi (asl daraxtga tegilmadi):

| Darvoza | Qaysi holatda qizil |
|---|---|
| «O'chirilgan partiyali qator FEFO'da ko'rinadi» | FEFO tuzatishidan oldingi commit (`EXISTS` sharti) |
| «Ikki yuza bir xil javob beradi» | O'sha commit |
| «Qoldiq yetmasa YARATISHDA xato» | `EnsureStockAvailableAsync` olib tashlanganda |

CI: `.github/workflows/ci.yml` ga `integration` job'i qo'shildi —
`bash scripts/run-tests.sh Release` (`dotnet test` EMAS).

---

## P2.1 — nom qidiruvi (2026-09-14)

«Сникерс» deb yozgan odam «Snikers» ni topa olmasdi: qidiruv mijozda `includes`
bilan ishlardi. `unaccent` bu ishni qilmaydi (u faqat diakritika), shuning uchun:

- `SearchNormalizer` (C#): kichik harf → kirill lotinga (o'zbek yozuvi) →
  apostrof tashlanadi → belgilar bo'sh joyga siqiladi. «Сникерс» → `snikers`.
- `name_search` ustuni + GIN trigram indeks; ustunni servislar, import va demo
  seed to'ldiradi; migratsiya eski qatorlarni backfill qiladi.
- **Backfill va RLS tuzog'i:** migratsiya `app_migrator` bilan yuradi va tenant
  jadvallarida `FORCE` RLS tufayli `UPDATE` JIM 0 qatorga tegardi (XATOLAR §9.3).
  Yechim — har jadval uchun `NO FORCE` → `UPDATE` → `FORCE`. Lokal stendda
  tekshirildi: bo'sh `name_search` qolgan qator **0 ta**.
- `ISearchService` nomzodlarni BAL bilan qaytaradi va «eng yaxshisi» ni
  tanlamaydi — A1 da AI bir nechta yaqin nomzodda qayta so'rashi uchun.
- UI: `GET /api/products?search=`, `GET /api/counterparties?search=`, 300 ms
  debounce; mahsulot turi filtri mijozda qoldi (backend uni bilmaydi).

**Ochiq (A1 dan oldin ko'rib chiqiladi):** GIN indeks so'rovda ishlatilmayapti
(seans chegarasi «snikers»↔«snickers» ni kesardi); transliteratsiya o'zbekcha
(ruscha `zh` past bal beradi); o'xshashlik chegarasi 0.4 — real katalogda sozlash
kerak bo'lishi mumkin.

---

## Ochiq qolgan ishlar (2026-09-14 holatiga)

| Ish | Kimda |
|---|---|
| Prod deploy (P0 №1) — baza zaxirasi + `F10_NameSearch` migratsiyasi bilan | 2026-09-15 ertalab |
| Bot tokenini BotFather'da `/revoke` va prod `wms.env` ga yangisi (P0 №4) | foydalanuvchi |
| Kabinet ekranlarini brauzerda ko'rish (3-BLOK №1) | foydalanuvchi |
| 2-BLOK: P2.2–P2.7 (P2.7 — Variant A qaroriga ko'ra) | keyingi sessiya |
| Qarz #17: WMS nginx CSP (3-BLOK №2) | keyingi sessiya |
