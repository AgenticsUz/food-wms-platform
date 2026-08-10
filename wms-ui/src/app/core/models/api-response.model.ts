export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  /**
   * Mashina o'qiy oladigan xato sababi. Xabar matni emas, aynan shu kod bo'yicha
   * qaror qabul qilinadi: `module_disabled:PRODUCTION`, `subscription_suspended`,
   * `trial_expired`, `tenant_inactive`, `limit_users`, `limit_warehouses`, `limit_transfers`.
   */
  code?: string;

  /**
   * Muvaffaqiyatli javobga ilashib keladigan ogohlantirish — **xato emas**
   * (HTTP 200/201 va `success: true` o'zgarmaydi).
   *
   * `ApiService` uni markazlashgan holda toast qiladi, komponentlar tegmaydi
   * ("bitta toast" qoidasi). Kodlari: `limit_warn_users` · `limit_warn_warehouses` ·
   * `limit_warn_transfers` · `permissions_outside_plan`.
   */
  warning?: ApiWarning | null;
}

export interface ApiWarning {
  code: string;
  message: string;
}
