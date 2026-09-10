/**
 * `/api/*` javob konverti — ESKI WMS shakli (PLATFORMA-TZ §7·F6.2 D15).
 *
 * ⚠️ Bu Wash/HRM ning `{ data, meta, errors }` konverti EMAS va `@agentics/http`
 * dagi `ApiClient` uchun yozilmagan. Muvaffaqiyatli ham, xato ham javob shu
 * shaklda keladi; farqi `success` da:
 *
 * ```json
 * { "success": true,  "data": {...}, "message": null, "code": null, "warning": null }
 * { "success": false, "data": null,  "message": "Omborda yetarli qoldiq yo'q", "code": null }
 * ```
 *
 * `message` server tomonidan `Accept-Language` bo'yicha ALLAQACHON tarjima
 * qilingan — frontend uni o'zi tarjima qilmaydi. Qaror esa `code` bo'yicha
 * qabul qilinadi (`module_disabled:PRODUCTION`, `trial_expired`, `limit_users`…).
 */
export interface ApiResponse<T> {
  readonly success: boolean;
  readonly data?: T | null;
  readonly message?: string | null;
  readonly code?: string | null;
  /**
   * Muvaffaqiyatli javobga ilashadigan ogohlantirish — XATO EMAS (HTTP 200/201
   * o'zgarmaydi). `ApiService` uni markazlashgan holda toast qiladi ("bitta
   * toast" qoidasi). Kodlar: `limit_warn_users` · `limit_warn_warehouses` ·
   * `limit_warn_transfers` · `permissions_outside_plan`.
   */
  readonly warning?: ApiWarning | null;
}

export interface ApiWarning {
  readonly code: string;
  readonly message: string;
}

/** Tana WMS konvertimi — `success` maydoni bor obyekt. */
export function isApiResponse(body: unknown): body is ApiResponse<unknown> {
  return (
    body !== null &&
    typeof body === 'object' &&
    typeof (body as { success?: unknown }).success === 'boolean'
  );
}
