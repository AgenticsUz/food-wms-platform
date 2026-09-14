# F10 — AI'dan OLDINGI ishlar (P bosqichi) — bajarish varag'i

> Sana: 2026-09-14. Bu — `WMS-AI-REJA.md` P bosqichining ISH VARAG'I: tartib,
> ustuvorlik va holat shu yerda yuritiladi; har ishning batafsil tavsifi va qabul
> mezoni REJA'ning ko'rsatilgan bo'limida. Sessiya ishni tugatgach shu faylda
> holatni yangilaydi (✅/⏳/❌ + sana + commit) va katta xulosalarni
> `docs/F10-HISOBOT.md` ga yozadi.
>
> **Tartib qoidasi:** 1-BLOK bajarilmagunicha A0/A1 boshlanmaydi. 2-BLOK A1 bilan
> parallel yoki A3'dan oldin. 3-BLOK ixtiyoriy/parallel — AI'ni bloklamaydi.

---

## 1-BLOK — AI'ni BLOKLAYDI (avval shular)

### 1.1 P0 dumlari (REJA §P0)

| # | Ish | Holat |
|---|---|---|
| 1 | T2 (`096f2be`) prod'ga deploy — F7 usuli, `docs/DEPLOY.md` (baza zaxirasi + image teglari shart) | ⏳ |
| 2 | §3 SQL tekshiruvi (quyida tayyor buyruqlar) → `01a0964f-…` sababi aniqlanadi | ⏳ |
| 3 | FEFO/o'chirilgan partiya tuzatishi (SQL natijasiga qarab, REJA §P1 oxiri va XATOLAR §3.4; darvoza testi: «o'chirilgan partiyali qator FEFO'da ko'rinadi») | ⏳ |
| 4 | Bot tokenini BotFather'da `/revoke`, yangi token prod `wms.env` ga (token suhbatlarda ko'ringan — xavfsizlik) | ⏳ |
| 5 | ~~Platforma + WMS push~~ | ✅ 2026-09-14 (`8fea6fe`, `096f2be`) |
| 6 | ~~T2 kod tuzatishi~~ | ✅ 2026-09-14 (`096f2be`) |

§3 SQL — foydalanuvchi `!` bilan yuboradi (Claude'ga prod o'qish ruxsati yo'q);
⚠️ hujjatdagi eski `-U wms -d wms` NOTO'G'RI, to'g'risi:

```
! ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200 "docker exec -i \$(docker ps -qf name=wms-postgres) psql -U postgres -d agentics_wms -c \"SELECT t.type, t.status, t.from_warehouse_id, i.product_id, i.quantity FROM wms.transfer t JOIN wms.transfer_item i ON i.transfer_id=t.id WHERE t.id='01a0964f-b5ea-724d-9791-86c8d4cce9b8';\""
```

```
! ssh -i ~/.ssh/agentics-demo deploy@45.138.158.200 "docker exec -i \$(docker ps -qf name=wms-postgres) psql -U postgres -d agentics_wms -c \"SELECT s.warehouse_id, s.batch_id, s.quantity, s.reserved_quantity, b.is_deleted FROM wms.warehouse_stock s LEFT JOIN wms.batch b ON b.id=s.batch_id WHERE s.product_id IN (SELECT product_id FROM wms.transfer_item WHERE transfer_id='01a0964f-b5ea-724d-9791-86c8d4cce9b8');\""
```

### 1.2 P1 — Backend test poydevori (REJA §P1) — ENG KATTA BO'SHLIQ

Hozir backend'da 0 ta test (faqat 5 frontend spec). AI hujjat yozishidan oldin
`TransferService`/FEFO testli bo'lishi shart.

| # | Ish | Holat |
|---|---|---|
| 1 | `tests/WMS.Tests` (xUnit + Testcontainers PostgreSQL, RLS haqiqiy bazada) + `AgenticsWms.slnx` ga ulash | ⏳ |
| 2 | Tenant konteksti test-yordamchisi | ⏳ |
| 3 | `TransferService` testlari (REJA'dagi 7 stsenariy: FEFO, «kerak N, mavjud M», kirim partiyasi, parallel 409, rad etish, komissiya, ruxsat 403) | ⏳ |
| 4 | `GetAvailableAsync` = `DeductFefoAsync` shart bir xilligi testi | ⏳ |
| 5 | `scripts/run-tests.sh` | ⏳ |

Qabul: ≥ 15 test yashil; kamida 3 tasi tuzatishsiz qizil berishi tekshirilgan.

### 1.3 P2.1 — Nom qidiruvi (REJA §P2.1) — A1 tool'lari uchun shart

| # | Ish | Holat |
|---|---|---|
| 1 | Migratsiya: `pg_trgm` extension, `name_search` ustunlari (C# transliteratsiya bilan to'ldiriladi — `unaccent` kirillni QILMAYDI), GIN trigram indeks | ⏳ |
| 2 | `ISearchService` (product/counterparty/warehouse, o'xshashlik bali bilan nomzodlar) | ⏳ |
| 3 | Mavjud UI qidiruvlari shu servisga o'tadi | ⏳ |

Qabul: «Snikers»/«snickers»/«Сникерс» — bitta mahsulot birinchi o'rinda.

---

## 2-BLOK — A1 bilan parallel / A3'dan OLDIN (REJA §P2.2–P2.7)

| # | Ish | Kimga kerak | Holat |
|---|---|---|---|
| 1 | P2.2 Oxirgi narx (`IPricingService`) | A1 `last_price`, asosan A3 qoralama | ⏳ |
| 2 | P2.3 Hujjat sanasi (`DocumentDate`, `transfers.backdate`; ⚠️ backfill RLS sabog'i — XATOLAR §9.3) | A3 «kecha kelgan kirim» | ⏳ |
| 3 | P2.4 Qisqa hujjat raqami (tenant hisoblagichi) | A3/UI qulayligi | ⏳ |
| 4 | P2.5 Partiya tannarxi (`Batch.UnitCost`) | A4 foyda hisoboti | ⏳ |
| 5 | P2.6 Standart ombor | AI qayta so'rashini kamaytiradi | ⏳ |
| 6 | P2.7 Qadoq — **FOYDALANUVCHI QARORI kerak** (A: `PackSize` maydoni / B: qilinmaydi) | A3 «50 quti» | ❓ qaror |

---

## 3-BLOK — Parallel / bloklamaydi

| # | Ish | Holat |
|---|---|---|
| 1 | Kabinet ekranlarini mijoz/agent sifatida brauzerda ko'rish — foydalanuvchi (Kaspersky sababli lokal brauzer tekshiruvi foydalanuvchida) | ⏳ |
| 2 | Qarz #17: WMS konteyner nginx'iga CSP (Console/HRM naqshi) | ⏳ |
| 3 | P3 skaner maydoni va etiketka (REJA §P3) — AI'ga bog'liq emas | ⏳ |

---

## Yakuniy darvoza (A0 ga o'tishdan oldin)

- [ ] 1-BLOK to'liq ✅ (P0 №1–4, P1, P2.1)
- [ ] `dotnet build` 0/0, `run-tests.sh` yashil, `wms-web` lint/test/build yashil
- [ ] Prod deploy qilingan va brauzerda ko'rilgan
- [ ] P2.7 qadoq qarori yozilgan (A yoki B)
- [ ] Shu fayl va `docs/F10-HISOBOT.md` yangilangan

Shundan keyin — `WMS-AI-REJA.md` §A0 (yangi sessiya, branch `f10-ai`).
