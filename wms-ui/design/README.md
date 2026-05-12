# 🍦 WMS UI Redesign — Claude Code Handoff Paketi

> Bu paket WMS Angular loyihasiga (wms-ui) yangi vizual tilni qo'llash uchun barcha kerakli fayllar va bosqichma-bosqich yo'riqnomani o'z ichiga oladi.
>
> **Vazifa:** hozirgi indigo SaaS palitrasini muzqaymoq/shirinlik fabrikasiga mos krem + pista + malina + karamel paletraga aylantirish. Strukturani o'zgartirmaslik — faqat ranglar, tipografika, kichik detallar.

---

## 📦 Paket tarkibi

```
handoff/
├── README.md                  ← shu fayl (Claude Code'ga bering)
├── 01-tokens.css              ← Tailwind v4 @theme + CSS o'zgaruvchilari
├── 02-primeng-overrides.css   ← PrimeNG kasr-ranglarni qayta belgilash
├── 03-fonts.html              ← <head>'ga qo'shiladigan font import
├── 04-status-pill.html        ← StatusBadgeComponent uchun yangi class'lar
├── 05-sample-sidebar.scss     ← sidebar.component.scss namunasi
├── 06-sample-dashboard.scss   ← dashboard.component.scss namunasi
└── tasks.md                   ← bosqichma-bosqich Claude Code vazifalari
```

---

## 🎨 Dizayn tizimi — qisqacha

### Ranglar
| Token | Hex | Maqsad |
|---|---|---|
| `--cream-50/100/200` | `#fbf8f3` → `#ede4d0` | Asosiy fon (oq o'rniga) |
| `--pistachio-500` | `#5e9540` | **Asosiy aksent — pista yashil** |
| `--berry-500` | `#d94c66` | Ikkilamchi — malina (chiqim, xato) |
| `--caramel-500` | `#c8902f` | Ogohlantirish — karamel sariq |
| `--blueberry-500` | `#4a6fb5` | Ma'lumot — moviy |
| `--cocoa-700` | `#2c2620` | Asosiy matn |

### Tipografika
- **Inter** — UI matn (hozirgicha)
- **Fraunces** — display fonti, faqat **katta raqamlar va sarlavhalar uchun** (KPI qiymatlari, dashboard hero)
- **JetBrains Mono** — partiya №, kod, monospace raqamlar

### Asosiy o'zgarishlar
1. Karta `border-radius`: `1rem` → `1.125rem` (yumshoqroq)
2. Asosiy fon: oq → krem (`#fbf8f3`)
3. Asosiy tugma: indigo-600 → cocoa-700 (matn rangi)
4. Aksent tugma: yo'q edi → pistachio-500 (yangi "Asosiy harakat")
5. Status pillalari: bg-emerald-100 → bg-pistachio-50 (boshqa palitrada)

---

## 🚀 Boshlash uchun — Claude Code'ga ushbu xabarni yuboring:

```
Bu wms-ui Angular loyihasiga yangi dizayn tizimini qo'llash bo'yicha topshiriq.

handoff/ papkasidagi fayllarni ko'rib chiqing va tasks.md fayldagi vazifalarni
ketma-ket bajaring. Strukturani o'zgartirmang — faqat ranglar, fontlar va
kichik vizual detallar yangilanadi.

Boshlash: handoff/README.md va handoff/tasks.md ni o'qing.
```

So'ngra `tasks.md`'dagi vazifalarni Claude Code o'zi bajaradi.
