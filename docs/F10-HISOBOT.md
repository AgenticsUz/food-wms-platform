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

Natija va qamrov — quyidagi «Yakuniy holat» bo'limida.
