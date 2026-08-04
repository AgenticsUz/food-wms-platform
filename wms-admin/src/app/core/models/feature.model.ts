/** Feature qiymati qayerdan kelgani — UI shuni ko'rsatadi. */
export type FeatureSource = 'tenant' | 'plan' | 'default';

/** Platforma katalogi. */
export interface FeatureInfo {
  code: string;
  name: string;
  description?: string | null;
  moduleCode: string;
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
  moduleCode: string;
  isEnabled: boolean;
  source: FeatureSource;
  note: string | null;
  isCustom?: boolean;
  ownerTenantId?: number | null;
  ownerTenantName?: string | null;
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
