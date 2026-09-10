# Custom features registry

> Bir mijoz uchun yozilgan har bir fitcha shu yerda qayd etiladi.
> Yozuvsiz custom fitcha yozilmaydi — ikki yildan keyin uni kim so'ragani, kimga
> kerakligi va o'chirsa bo'ladimi degan savolga javob shu jadvaldan chiqadi.

**Hozircha bo'sh.** Skelet va qoidalar tayyor (S5), birinchi so'rov kelganda to'ldiriladi.

---

## Qoidalar (kod darajasida majburlanadi)

| # | Qoida | Qayerda tekshiriladi |
|---|---|---|
| 1 | Kod `custom.` bilan boshlanadi | `FeatureService.CreateAsync` |
| 2 | `IsCustom = true` → `DefaultEnabled = false` bo'lishi **shart** | `FeatureService.CreateAsync` |
| 3 | `OwnerTenantId` majburiy — kim uchun yozilgani | `FeatureService.CreateAsync` |
| 4 | Custom fitcha **hech qanday planga** qo'shilmaydi | `PlanService.ValidateFeatureCodesAsync` |
| 5 | Har custom controller `[RequireFeature("custom.<kod>")]` bilan | code review |
| 6 | Mantiq `WMS.Application/Features/Custom/<kod>/` ichida, umumiy servislarda emas | code review |

Yoqish (F6 dan beri — Console'ning WMS bo'limi): `PUT /admin/v1/tenants/{id}/features`
→ `[{ "code": "custom.x", "enabled": true, "note": "..." }]` (`enabled: null` — override'ni olib tashlaydi).

---

## Reestr

| Kod | Mijoz (tenant) | Sana | Sabab | So'ragan | Fayllar | 3-mijoz so'rasa umumiyga? |
|---|---|---|---|---|---|---|
| _(bo'sh)_ | | | | | | |

### Yozuv shabloni

```
| custom.<kod> | <Tenant nomi> (#id) | 2026-__-__ | <nima uchun kerak bo'ldi> |
  <kim so'radi> | WMS.Application/Features/Custom/<kod>/, WMS.API/Controllers/Custom/<X>Controller.cs | ha / yo'q |
```

**"3-mijoz so'rasa umumiyga ko'chiriladi"** ustuni ataylab: uchinchi mijoz so'ragan narsa
endi maxsus emas, mahsulotning bir qismi. O'sha paytda fitcha umumiy kodga ko'chiriladi,
plan(lar)ga qo'shiladi va bu qatordan o'chiriladi.
