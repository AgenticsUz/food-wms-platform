import { tenantStatusOf, toPlatformUser } from './me-adapter';
import type { WmsMe, WmsSubscription } from './wms-me.model';

const subscription = (patch: Partial<WmsSubscription> = {}): WmsSubscription => ({
  status: 'Active',
  allowed: true,
  code: null,
  message: null,
  publicMessage: null,
  planCode: 'pro',
  planName: 'Pro',
  trialEndsAt: null,
  paidUntil: '2026-12-31T00:00:00Z',
  trialDaysLeft: null,
  paidDaysLeft: 100,
  ...patch,
});

const me = (patch: Partial<WmsMe> = {}): WmsMe => ({
  sub: 'b7f1c1e2-0000-7000-8000-000000000001',
  profileId: null,
  fullName: 'Ali Valiyev',
  phone: '+998901234567',
  isPlatformAdmin: false,
  tenant: {
    id: 't-1',
    code: 'demo',
    name: 'Demo',
    logoUrl: null,
    logoSquareUrl: null,
    brandColor: '#112233',
  },
  roles: ['admin'],
  permissions: ['warehouse.view'],
  modules: ['WAREHOUSE_RAW'],
  features: ['warehouse.batches'],
  subscription: subscription(),
  ...patch,
});

describe('tenantStatusOf', () => {
  it('`allowed` hal qiluvchi: Trial holatida ham muddati o\'tsa — expired', () => {
    expect(tenantStatusOf(subscription({ status: 'Trial', allowed: false, code: 'trial_expired' }))).toBe('expired');
    expect(tenantStatusOf(subscription({ allowed: false, code: 'suspended_other' }))).toBe('suspended');
  });

  it('ruxsat etilgan Trial → trial, qolgani → active', () => {
    expect(tenantStatusOf(subscription({ status: 'Trial' }))).toBe('trial');
    expect(tenantStatusOf(subscription({ status: 'Suspended', allowed: true }))).toBe('active');
  });

  it('obuna kelmasa holat yo\'q — ichkariga qo\'yilmaydi (fail-closed)', () => {
    expect(tenantStatusOf(null)).toBeNull();
  });
});

describe('toPlatformUser', () => {
  it('`sub` → `id`, modullar va obuna `tenant` ichiga, asl javob `wms` da', () => {
    const user = toPlatformUser(me());
    expect(user.id).toBe('b7f1c1e2-0000-7000-8000-000000000001');
    expect(user.tenant?.status).toBe('active');
    expect(user.tenant?.modules).toEqual(['WAREHOUSE_RAW']);
    expect(user.tenant?.features).toEqual(['warehouse.batches']);
    expect(user.wms.roles).toEqual(['admin']);
  });

  it('ochiq xabar: `publicMessage` `message` dan ustun', () => {
    const blocked = toPlatformUser(
      me({ subscription: subscription({ allowed: false, message: 'server', publicMessage: 'admin' }) })
    );
    expect(blocked.tenant?.suspendedPublicMessage).toBe('admin');
  });

  it('tenant yo\'q — `tenant: null` (hisob biriktirilmagan)', () => {
    expect(toPlatformUser(me({ tenant: null })).tenant).toBeNull();
  });
});
