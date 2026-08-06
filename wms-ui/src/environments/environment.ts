export const environment = {
  production: false,
  apiUrl: 'http://localhost:7040/api',
  tenantSlug: 'admin',
  // Faqat ZAXIRA. Haqiqiy qiymat serverdan keladi (`Support:Phone` / `Support:Email`,
  // `/api/public/branding` va `/api/subscription/me` orqali) — raqamni almashtirish uchun
  // frontendni qayta yig'ish shart emas. Bu yerdagi qiymatlar server javob bermaganda
  // ishlatiladi, shuning uchun bo'sh qoldirilmaydi.
  supportPhone: '+998 90 000 00 00',
  supportEmail: 'support@wms.uz'
};
