import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { PortalService } from '../../../core/services/portal.service';

@Component({
  selector: 'app-portal-layout',
  standalone: true,
  imports: [RouterOutlet, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-shell" *transloco="let t">
      <header class="portal-header">
        <div class="portal-header-left">
          <i class="pi pi-warehouse portal-logo"></i>
          <span class="portal-brand">WMS {{ t('partners.portal') }}</span>
        </div>
        <div class="portal-header-right">
          <span class="portal-user">{{ portalService.counterparty()?.name ?? 'Guest' }}</span>
          <button class="portal-logout" (click)="portalService.logout()">
            <i class="pi pi-sign-out"></i> {{ t('auth.logout') }}
          </button>
        </div>
      </header>
      <main class="portal-content page-enter">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .portal-shell { min-height: 100vh; background: var(--bg-base); }
    .portal-header {
      height: 64px; display: flex; align-items: center; justify-content: space-between;
      padding: 0 24px; background: var(--bg-surface); border-bottom: 1px solid var(--border);
      position: sticky; top: 0; z-index: 10;
    }
    .portal-header-left { display: flex; align-items: center; gap: 10px; }
    .portal-logo { font-size: 24px; color: var(--primary-500); }
    .portal-brand { font-size: 18px; font-weight: 700; color: var(--text-primary); }
    .portal-header-right { display: flex; align-items: center; gap: 16px; }
    .portal-user { font-size: 14px; font-weight: 600; color: var(--text-primary); }
    .portal-logout {
      background: none; border: none; color: var(--text-muted); cursor: pointer;
      display: flex; align-items: center; gap: 6px; font-size: 13px; padding: 6px 12px;
      border-radius: 6px; transition: all 0.15s;
      &:hover { background: var(--bg-muted); color: var(--danger); }
    }
    .portal-content { padding: 24px; max-width: 1200px; margin: 0 auto; }
  `]
})
export default class PortalLayoutComponent {
  portalService = inject(PortalService);
}
