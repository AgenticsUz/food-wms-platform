/**
 * Feature qiymati qayerdan kelgani — UI shuni ko'rsatadi.
 * `module` — moduli o'chirilgani uchun majburan o'chiq (feature moduldan o'ta olmaydi).
 */
export type FeatureSource = 'tenant' | 'plan' | 'default' | 'module';

/** Platforma katalogi. */
export interface FeatureInfo {
  code: string;
  name: string;
  description?: string | null;
  /** null — modulga tegishli emas (export, import, analitika). */
  moduleCode: string | null;
  defaultEnabled: boolean;
  isCustom: boolean;
  sortOrder: number;
  /** Custom feature kim uchun yozilgan. */
  ownerTenantId?: number | null;
  ownerTenantName?: string | null;
  requestedAt?: string | null;
  reason?: string | null;
}

/** Tenant uchun yechilgan holat. */
export interface TenantFeature {
  code: string;
  name: string;
  /** null — modulga tegishli emas; UI'da "GENERAL" guruhida ko'rinadi. */
  moduleCode: string | null;
  isEnabled: boolean;
  source: FeatureSource;
  note: string | null;
  isCustom?: boolean;
  ownerTenantId?: number | null;
  ownerTenantName?: string | null;
  requestedAt?: string | null;
  reason?: string | null;
}

/** `isEnabled: null` — override olib tashlanadi, plan qiymatiga qaytadi. */
export interface FeatureOverrideDto {
  code: string;
  isEnabled: boolean | null;
  note: string | null;
}

export interface UpdateFeaturesDto {
  features: FeatureOverrideDto[];
}
