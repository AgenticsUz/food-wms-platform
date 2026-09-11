import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter, type Route } from '@angular/router';
import { AuthService, CurrentUserStore, type CurrentUser } from '@agentics/auth';
import { LOCALE_SOURCE } from '@agentics/http';
import { LanguageService } from '@agentics/i18n';
import { AccessAccountsPage, IdentityTenantI18n } from '@agentics/identity-tenant';

import type { WmsCurrentUser, WmsMe } from '../../core/auth/wms-me.model';
import { provideWmsIdentityTenant } from '../../core/i18n/identity-tenant';
import { BrandingService } from '../../core/branding/branding.service';
import { NotificationService } from '../../core/notify/notification.service';
import { SETTINGS_NAV } from '../../layout/nav';
import { SidebarComponent } from '../../layout/sidebar/sidebar.component';
import { SETTINGS_ROUTES } from './settings.routes';

/**
 * F8.1 «Kirish hisoblari»: marshrut paket sahifasini yuklaydi, menyu bandi va
 * guard FAQAT Identity `admin` roliga ochiq (API `/tenant/v1` ham shuni talab
 * qiladi — bu UX, chegara Identity'da).
 */
function me(roles: readonly string[], permissions: readonly string[] = ['settings.roles']): WmsMe {
  return {
    sub: 'b7f1c1e2-0000-7000-8000-000000000001',
    profileId: null,
    fullName: 'Ali Valiyev',
    phone: null,
    isPlatformAdmin: false,
    tenant: null,
    roles,
    permissions,
    modules: [],
    features: [],
    subscription: null,
  };
}

function signIn(roles: readonly string[]): void {
  const wms = me(roles);
  const user = {
    id: wms.sub,
    fullName: wms.fullName,
    isPlatformAdmin: false,
    permissions: wms.permissions,
    wms,
  } as unknown as WmsCurrentUser;
  TestBed.inject(CurrentUserStore).setUser(user as CurrentUser);
}

const accessRoute = (): Route => {
  const route = SETTINGS_ROUTES.find((r) => r.path === 'access');
  if (route === undefined) {
    throw new Error('settings/access marshruti yo\'q');
  }
  return route;
};

function runGuard(): unknown {
  const guard = accessRoute().canActivate?.[0] as () => unknown;
  return TestBed.runInInjectionContext(() => guard());
}

function settingsMenuKeys(): string[] {
  const sidebar = TestBed.runInInjectionContext(() => new SidebarComponent());
  return sidebar.visibleChildren(SETTINGS_NAV.children).map((c) => c.key);
}

describe('Sozlamalar → Kirish hisoblari', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: {} },
        { provide: NotificationService, useValue: {} },
        { provide: LanguageService, useValue: { language: signal('uz-Latn') } },
        {
          provide: BrandingService,
          useValue: { wideLogoSrc: signal(null), squareLogoSrc: signal(null), tenantName: signal(null) },
        },
      ],
    });
  });

  it('`settings/access` paketning AccessAccountsPage sahifasini yuklaydi', async () => {
    const route = accessRoute();
    expect(route.data?.['titleKey']).toBe('settings.accessAccounts');
    const component = await (route.loadComponent as () => Promise<unknown>)();
    expect(component).toBe(AccessAccountsPage);
  });

  it('admin: menyuda «Rollar» yonida ko\'rinadi va guard o\'tkazadi', () => {
    signIn(['admin']);
    const keys = settingsMenuKeys();
    expect(keys).toContain('settings.accessAccounts');
    expect(keys.indexOf('settings.accessAccounts')).toBe(keys.indexOf('settings.roles') + 1);
    expect(runGuard()).toBe(true);
  });

  it('admin bo\'lmasa (manager, hatto settings.roles ruxsati bilan): menyuda yo\'q, guard /forbidden', () => {
    signIn(['manager']);
    expect(settingsMenuKeys()).not.toContain('settings.accessAccounts');
    // Ruxsat kodi rolni almashtirmaydi: «Rollar» ko'rinadi, «Kirish hisoblari» — yo'q.
    expect(settingsMenuKeys()).toContain('settings.roles');

    const result = runGuard();
    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/forbidden');
  });

  it('paket tili WMS tilidan olinadi (LOCALE_SOURCE emas) va almashsa darhol ergashadi', () => {
    const language = signal('ru');
    TestBed.overrideProvider(LanguageService, { useValue: { language } });
    TestBed.configureTestingModule({
      providers: [
        provideWmsIdentityTenant(),
        { provide: LOCALE_SOURCE, useValue: { current: signal('uz-Latn') } },
      ],
    });
    const i18n = TestBed.inject(IdentityTenantI18n);
    expect(i18n.language()).toBe('ru');
    language.set('uz-Cyrl');
    expect(i18n.language()).toBe('uz-Cyrl');
  });
});
