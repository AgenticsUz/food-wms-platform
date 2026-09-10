import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '@agentics/auth';
import { TenantStore } from '@agentics/tenant';

import { blockedReasonKey } from '../../core/api/wms-api-error';
import { WmsSession } from '../../core/auth/wms-session';

/**
 * Tenant holati ilovani to'sganda — qobiqdan TASHQARIDA (`tenantGuard` yo'naltiradi).
 *
 * IKKI holat, bitta sahifa (Wash `tenant-state.page` naqshi):
 *
 *  - `blocked` (`/subscription`) — obuna to'xtatilgan: `/api/me` da
 *    `subscription.allowed = false` yoki sessiya davomida 402. Ko'rsatiladi:
 *    sabab (`code` → tarjima), administratorning ochiq matni
 *    (`publicMessage ?? message` — `TenantStore.blockMessage`), tarif.
 *    «Faqat o'qish» rejimi YO'Q: WMS backendi bloklangan tenantning o'qish
 *    so'rovlariga ham 402 qaytaradi, ya'ni u rejim bo'sh ekranlar bo'lardi.
 *  - `unknown` (`/select-tenant`) — `/me` javob bermadi (vaqtincha, qayta urinish
 *    ma'noli) YOKI hisob hech qaysi tashkilotga bog'lanmagan (`notLinked` —
 *    doimiy, qayta urinish tugmasi YO'Q).
 *
 * Tenant TANLASH yo'q: tenant tokendagi `tenant_id` dan keladi.
 */
@Component({
  selector: 'app-tenant-state-page',
  imports: [TranslocoDirective, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tenant-state.page.html',
  styleUrl: '../state-card.scss',
})
export class TenantStatePage {
  private readonly auth = inject(AuthService);
  private readonly tenants = inject(TenantStore);
  private readonly session = inject(WmsSession);
  private readonly router = inject(Router);

  /** Marshrut `data` sidan: `blocked` | `unknown`. */
  readonly kind = input<'blocked' | 'unknown'>('unknown');

  protected readonly busy = signal(false);
  protected readonly tenantName = this.tenants.name;
  protected readonly message = this.tenants.blockMessage;
  protected readonly planName = computed(() => this.session.subscription()?.planName ?? null);
  protected readonly reasonKey = computed(() => blockedReasonKey(this.session.blockCode()));

  protected readonly notLinked = computed(() => this.kind() === 'unknown' && this.tenants.notLinked());
  /** Tarjima ildizi: `blocked` | `unknown` | `unlinked`. */
  protected readonly stateKey = computed(() => (this.notLinked() ? 'unlinked' : this.kind()));

  protected async retry(): Promise<void> {
    this.busy.set(true);
    try {
      await firstValueFrom(this.auth.loadCurrentTenant());
      if (this.tenants.isReady()) {
        this.session.clearBlock();
        await this.router.navigateByUrl('/');
      }
    } finally {
      this.busy.set(false);
    }
  }

  protected signOut(): void {
    void this.auth.logout('manual');
  }
}
