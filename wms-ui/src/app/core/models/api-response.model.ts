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
}
