export const environment = {
  production: true,
  // Nisbiy — `app.warehouse-system.uz` dagi nginx `/api` ni backendga proxy qiladi.
  // CORS ishga tushmaydi (same-origin), HTTPS'da mixed-content bloklanmaydi.
  apiUrl: '/api',
  // `app.` va `admin.` — xizmat subdomenlari, tenant emas (`tenant-slug.util.ts`).
  // Shuning uchun login formasi tashkilot kodini so'raydi. Har mijozga o'z subdomeni
  // berilganda (R20) slug host'dan o'zi olinadi va bu qiymat ishlatilmay qoladi.
  tenantSlug: 'admin',
  // Obuna bloklanganda / plan o'zgartirmoqchi bo'lganda ko'rsatiladigan aloqa
  // Faqat ZAXIRA — haqiqiy qiymat serverning `Support:Phone` / `Support:Email`
  // sozlamasidan keladi. Batafsil: `environment.ts` dagi izoh.
  supportPhone: '+998 90 014 31 36',
  supportEmail: 'support@wms.uz'
};
