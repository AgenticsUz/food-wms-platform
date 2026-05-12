# 📋 Claude Code uchun vazifalar (tasks.md)

> Ushbu vazifalarni ketma-ket bajaring. Har bir bosqichdan keyin `ng build` qiling — 0 ta xato bo'lishi shart.

---

## ✅ Bosqich 0 — Tayyorgarlik

1. Loyihani branch'ga oling: `git checkout -b feature/visual-redesign`
2. `handoff/` papkasini diqqat bilan o'qing
3. Hozirgi `src/styles.scss` ni `src/styles.scss.backup` deb saqlang

---

## ✅ Bosqich 1 — Fontlar

**Fayl:** `src/index.html`

`<head>` ichidagi mavjud font link'larini olib tashlang va `handoff/03-fonts.html` dagi blokni qo'shing (Inter + Fraunces + JetBrains Mono).

---

## ✅ Bosqich 2 — Tokenlar

**Fayl:** `src/styles.scss`

1. Mavjud `@import "tailwindcss"` qatorini olib tashlang
2. `handoff/01-tokens.css` ichidagi to'liq blokni `src/styles.scss` boshiga qo'ying
3. Keyin `handoff/02-primeng-overrides.css` ichidagi blokni qo'shing
4. Eski custom CSS'larni saqlab qoling, lekin quyidagilarni almashtiring:
   - `bg-indigo-*` → `bg-pistachio-*`
   - `text-indigo-*` → `text-pistachio-*`
   - `border-indigo-*` → `border-pistachio-*`

**Test:** `ng serve` → istalgan sahifa krem fonda, pista yashil tugmalar bilan ochilishi kerak.

---

## ✅ Bosqich 3 — StatusBadgeComponent

**Fayl:** `src/app/shared/components/status-badge.component.ts` (yoki `.html`)

Hozirgi class'larni quyidagiga moslang:

| Eski (Tailwind) | Yangi (custom pill) |
|---|---|
| `bg-emerald-100 text-emerald-800 ...` | `pill pill-success` |
| `bg-amber-100 text-amber-800 ...` | `pill pill-warning` |
| `bg-red-100 text-red-800 ...` | `pill pill-danger` |
| `bg-blue-100 text-blue-800 ...` | `pill pill-info` |
| `bg-gray-100 text-gray-600 ...` | `pill pill-neutral` |

Misol:
```html
<span class="pill pill-success">{{ label }}</span>
```

---

## ✅ Bosqich 4 — Sidebar

**Fayl:** `src/app/layout/sidebar/sidebar.component.scss`

Hozirgi sidebar'ni "to'q rang"dan **oq + krem hover** ga o'zgartiramiz:

```scss
.sidebar {
  background: white;                              /* edi: dark */
  border-right: 1px solid var(--border-subtle);
  width: 248px;
}

.logo-mark {
  background: linear-gradient(140deg, var(--color-pistachio-200), var(--color-pistachio-500));
  color: white;
  border-radius: 10px;
  box-shadow: 0 4px 12px rgba(94, 149, 64, 0.25);
}

.nav-item {
  color: var(--color-cocoa-500);
  font-weight: 500;
  border-radius: 8px;
  padding: 8px 10px;

  &:hover { background: var(--color-cream-50); }

  &.active {
    background: var(--color-cream-100);
    color: var(--color-cocoa-700);
    font-weight: 600;
    position: relative;

    &::before {
      content: "";
      position: absolute; left: -10px; top: 8px; bottom: 8px;
      width: 3px; background: var(--color-pistachio-500);
      border-radius: 0 3px 3px 0;
    }
  }
}

.user-avatar {
  background: linear-gradient(135deg, var(--color-berry-300), var(--color-berry-500));
  color: white;
}
```

Brand matnini **"WMS Platform"** dan **"Plombir WMS"** ga (yoki tenant nomiga) o'zgartirish — `sidebar.component.html`'da.

---

## ✅ Bosqich 5 — Dashboard

**Fayl:** `src/app/modules/dashboard/dashboard.component.html` + `.scss`

### 5.1. Hero greeting strip qo'shing
Mavjud `dashboard-header` ustiga **iliq salomlashish bloki**:

```html
<div class="hero-greeting">
  <div class="hero-content">
    <span class="hero-label">{{ greetingLabel }} 🍦</span>
    <h1 class="display">
      Bugun zavodda <span class="accent">{{ todayPlanKg }}</span> kg
      muzqaymoq ishlab chiqarish rejalashtirilgan.
    </h1>
    <p class="hero-sub">{{ heroSubtitle }}</p>
  </div>
</div>
```

```scss
.hero-greeting {
  background: linear-gradient(120deg, #fbf8f3 0%, #f0f7ec 50%, #fdf2f4 100%);
  border-radius: 18px;
  padding: 22px 26px;
  margin-bottom: 20px;
  border: 1px solid var(--border-subtle);
}
.hero-greeting .display {
  font-family: var(--font-display);
  font-size: 30px;
  line-height: 1.1;
}
.hero-greeting .accent { color: var(--color-pistachio-600); }
```

### 5.2. KPI kartalar
Hozirgi 5 ta `summary-card` ni quyidagi sxemaga keltiring:

- Icon konteyner: `width: 38px; height: 38px; border-radius: 11px;` rangli soft fon
- Label: `text-[11px] font-semibold uppercase tracking-wide text-cocoa-400`
- Value: `font-family: var(--font-display); font-size: 28px;`
- Trend rozetka: `bg-pistachio-50 text-pistachio-700` yoki `bg-berry-50 text-berry-700`
- Karta tagiga **sparkline** qo'shing (ApexCharts mini line, height: 32, no axis)

Ranglar mapping:
- Total Stock → pistachio
- Efficiency → blueberry
- Pending Transfers → caramel
- Revenue → berry
- Low Stock → berry (alert)

### 5.3. ApexCharts ranglarini yangilash
**Fayl:** `src/app/core/config/apex-defaults.ts`

```ts
export const APEX_DEFAULTS = {
  chart: { fontFamily: 'Inter, sans-serif', toolbar: { show: false } },
  colors: ['#5e9540', '#d94c66', '#c8902f', '#4a6fb5', '#8a7e6a', '#7eb35a'],
  grid: { borderColor: 'rgba(99,88,70,0.08)', strokeDashArray: 4 },
  tooltip: { theme: 'light' },
  stroke: { curve: 'smooth', width: 2 },
};
```

---

## ✅ Bosqich 6 — Cards (umumiy)

Loyihadagi barcha karta klasslarini topib (`.summary-card`, `.chart-card`, `.report-card`, `.ext-card` va h.k.), umumiy `wms-card` mixin'ga keltiring:

```scss
%wms-card {
  background: white;
  border: 1px solid var(--border-subtle);
  border-radius: 16px;
  box-shadow: 0 2px 6px rgba(99,88,70,0.06);
  transition: all 0.15s ease;

  &:hover { box-shadow: 0 6px 20px rgba(99,88,70,0.08); }
}
.summary-card, .chart-card, .report-card, .ext-card { @extend %wms-card; }
```

---

## ✅ Bosqich 7 — Login sahifasi

**Fayl:** `src/app/modules/auth/login/login.component.html` + `.scss`

Hozirgi single-column login'ni **two-column** layoutga o'tkazing:

- Chap (50%): hero gradient (`linear-gradient(150deg, #f0f7ec 0%, #fbf8f3 50%, #fce4e8 100%)`) + brend + slogan
- O'ng (50%): forma

Slogan namunasi:
> Har bir partiyani tomchidan qutigacha kuzatib boring.

Form input'lar `border-radius: 10px`, focus ring `--color-pistachio-500`.
Submit tugma: `pill pill-success` formatida — to'liq kenglik.

---

## ✅ Bosqich 8 — Transfers

**Fayl:** `src/app/modules/transfers/transfer-list.component.html`

Jadvalni **kartali grid**ga aylantiring (2 ustun):

- Har bir transfer karta — yo'nalish ikonasi (kirim/chiqim/ichki, rangli)
- Ichida "Qayerdan → Qayerga" pillalari
- Pastida: mahsulot soni, miqdor, summa, vaqt
- Shoshilinch transferlar uchun yuqorida qizil "SHOSHILINCH" lenta

---

## ✅ Bosqich 9 — Production (Kanban)

**Fayl:** `src/app/modules/production/order-list/order-list.component.html`

Buyurtmalar ro'yxatini **Kanban**ga o'tkazing:
- 4 ustun: Loyiha · Boshlandi · Sifat nazorati · Tayyor
- Har bir karta: kod, mahsulot, miqdor, retsept №, progress bar (boshlandi uchun), QC belgi (sifat nazoratida)
- Yuqorida **jonli smena** indikatori (yashil pulse dot bilan)

---

## ✅ Bosqich 10 — i18n yangilanishi

**Fayl:** `src/assets/i18n/uz.json`, `ru.json`, `en.json`

Yangi kalitlar qo'shing:

```json
{
  "dashboard.heroSubtitle": "Ertalabki smena ... · 3 ta kutilayotgan transfer",
  "transfer.urgent": "Shoshilinch",
  "production.liveShift": "Jonli · Ertalabki smena",
  "common.fromTo": "Qayerdan → Qayerga"
}
```

---

## ✅ Bosqich 11 — Test va sayqallash

1. `ng build` → 0 xato
2. Har sahifani ko'rib chiqing: dashboard, ombor, transferlar, ishlab chiqarish, login
3. Tema almashtiruvchi (light) ishlayotganini tekshiring
4. Til almashtiring: uz/ru/en — barcha matn tarjima qilinadi
5. Mobil ko'rinish (< 768px): sidebar kollapsi, jadvallar overflow

---

## 🎯 Mezonlar

- [ ] Hech qaysi sahifada hardcoded indigo/blue rang qolmagan
- [ ] Asosiy fon krem (`#fbf8f3`)
- [ ] KPI raqamlar Fraunces fontida
- [ ] Status pillalari yangi palitrada
- [ ] Login sahifasi two-column hero bilan
- [ ] Sidebar oq fonda, krem hover
- [ ] PrimeNG dialoglar/dropdownlar avtomatik yangi rangda

---

## 📎 Reference

Vizual maket: [WMS Redesign.html](../WMS%20Redesign.html) — bitta HTML fayl, barcha sahifalar yonma-yon. `Plombir/redesign/` papkasidagi `tokens.css`, `components.jsx`, `dashboard.jsx`, `pages.jsx`, `production.jsx` — to'liq prototipning manba kodi.

Savollar bo'lsa: token nomlari, ranglar yoki komponent strukturasi haqida `handoff/01-tokens.css` va `WMS Redesign.html`'dagi React kodga qarang.
