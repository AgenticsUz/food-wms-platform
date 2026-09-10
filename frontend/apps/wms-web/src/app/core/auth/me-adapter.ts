import type { TenantInfo, TenantStatus } from '@agentics/tenant';

import type { WmsCurrentUser, WmsMe, WmsSubscription } from './wms-me.model';

/**
 * Obuna holati → platforma `TenantStatus` i.
 *
 * `@agentics/tenant` ning `tenantGuard` i faqat `active`/`trial` ni ichkariga
 * qo'yadi, `suspended`/`expired` ni obuna ekraniga yuboradi, holat umuman
 * bo'lmasa — «tenant noma'lum» ekraniga. Shuning uchun:
 *
 *  - `allowed` — HAL QILUVCHI maydon. Holat `Trial` bo'lib turib ham sinov
 *    muddati o'tgan bo'lishi mumkin (`trial_expired`); qaror serverniki
 *    (`SubscriptionPolicy`), frontend sanalarni qayta hisoblamaydi.
 *  - `subscription` umuman kelmasa `null` — ya'ni «noma'lum», ichkariga
 *    qo'yilmaydi (fail-closed). Shartnomada u doim bor; yo'qligi backend
 *    nosozligi va u ko'rinib turishi kerak, jimgina «faol» deb o'tmasligi.
 */
export function tenantStatusOf(subscription: WmsSubscription | null): TenantStatus | null {
  if (subscription === null) {
    return null;
  }
  if (!subscription.allowed) {
    return subscription.code === 'trial_expired' || subscription.code === 'payment_expired'
      ? 'expired'
      : 'suspended';
  }
  return subscription.status === 'Trial' ? 'trial' : 'active';
}

/**
 * WMS `/me` → platforma `CurrentUser` + `TenantInfo`.
 *
 * ⚠️ Nega moslash paket ichida emas, SHU YERDA: `AuthService.loadCurrentUser()`
 * javobni `CurrentUserStore.setUser()` va `TenantStore.setTenant(user.tenant)`
 * ga TO'G'RIDAN-TO'G'RI uzatadi. WMS shartnomasida esa id `sub` deb ataladi,
 * modullar va obuna `tenant` dan tashqarida. Moslanmasa `tenantGuard` holatni
 * ko'rmay har foydalanuvchini «tenant noma'lum» ekranida qoldirardi.
 * Asl javob `wms` maydonida o'zgarishsiz qoladi — WMS kodi faqat shuni o'qiydi.
 */
export function toPlatformUser(me: WmsMe): WmsCurrentUser {
  const subscription = me.subscription ?? null;
  const tenant: TenantInfo | null =
    me.tenant === null
      ? null
      : {
          id: me.tenant.id,
          name: me.tenant.name,
          code: me.tenant.code,
          status: tenantStatusOf(subscription) ?? undefined,
          modules: me.modules ?? [],
          features: me.features ?? [],
          planCode: subscription?.planCode ?? null,
          trialEndsAt: subscription?.trialEndsAt ?? null,
          paidUntil: subscription?.paidUntil ?? null,
          suspendedPublicMessage: subscription?.publicMessage ?? subscription?.message ?? null,
        };

  return {
    id: me.sub,
    fullName: me.fullName,
    isPlatformAdmin: me.isPlatformAdmin,
    permissions: me.permissions ?? [],
    phone: me.phone ?? undefined,
    profileId: me.profileId ?? undefined,
    tenant,
    wms: me,
  };
}
