export const environment = {
  production: true,
  // Nisbiy — `admin.warehouse-system.uz` dagi nginx `/api` ni backendga proxy qiladi.
  // Shu sababli CORS umuman ishga tushmaydi (same-origin) va HTTPS'ga o'tilganda
  // mixed-content bloklanmaydi. `wms-ui` ham xuddi shunday ishlaydi.
  apiUrl: '/api'
};
