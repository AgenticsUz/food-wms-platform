export const environment = {
  production: true,
  // Nginx orqasida relative — HTTPS'da mixed-content bloklanmasin (CLAUDE.md)
  apiUrl: '/api',
  tenantSlug: 'admin',
  // Obuna bloklanganda / plan o'zgartirmoqchi bo'lganda ko'rsatiladigan aloqa
  // Faqat ZAXIRA — haqiqiy qiymat serverning `Support:Phone` / `Support:Email`
  // sozlamasidan keladi. Batafsil: `environment.ts` dagi izoh.
  supportPhone: '+998 90 000 00 00',
  supportEmail: 'support@wms.uz'
};
