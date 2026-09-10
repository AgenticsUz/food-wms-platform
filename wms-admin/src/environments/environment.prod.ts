export const environment = {
  production: true,
  // Nisbiy — `admin.warehouse-system.uz` dagi nginx `/api` ni backendga proxy qiladi.
  // Shu sababli CORS umuman ishga tushmaydi (same-origin) va HTTPS'ga o'tilganda
  // mixed-content bloklanmaydi. `wms-ui` ham xuddi shunday ishlaydi.
  //
  // SHART: nginx'da `location /api` bloki bo'lishi kerak (`docs/CLAUDE.md` §6).
  // Usiz `/api/...` SPA fallback'ga tushadi — GET'da index.html, POST'da nginx 405.
  // `proxy_pass` oxirida slash BO'LMASIN: backend marshruti `api/auth/...`, ya'ni
  // yo'l o'zgarmay uzatilishi kerak.
  apiUrl: '/api'
};
