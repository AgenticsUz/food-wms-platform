import { ChangeDetectionStrategy, Component, computed, inject, input, signal, type OnInit } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import {
  TemporaryPasswordDialog,
  TenantApiError,
  TenantMembersApi,
  normalizePhone,
  type TemporaryPasswordCredentials,
} from '@agentics/identity-tenant';
import { Button } from 'primeng/button';

import { ApiService } from '../../../core/api/api.service';
import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import type { PortalAccount } from '../../../features/portal/portal.model';

/**
 * Kontragent/agent kartasidagi «Kabinet» kartochkasi (F9).
 *
 * Oqim IKKI qadam va ikki xizmat:
 *  1. Identity (`/tenant/v1/members`, `@agentics/identity-tenant`) — hisob ochiladi,
 *     javobda BIR MARTALIK vaqtinchalik parol keladi;
 *  2. WMS (`PUT .../portal-account`) — qaysi hisob qaysi kartaga tegishli, yoziladi.
 *
 * ⚠️ Parol WMS'ga YUBORILMAYDI va hech qayerda saqlanmaydi (P4): u faqat shu
 * dialogda ko'rsatiladi. Ikkinchi qadam yiqilsa hisob Identity'da qolib ketadi —
 * shuning uchun xato matnida «hisob ochildi, biriktirib bo'lmadi» deyiladi va
 * admin «Kirish hisoblari» dan ko'ra oladi.
 */
@Component({
  selector: 'app-portal-account-card',
  imports: [TranslocoDirective, Button, TemporaryPasswordDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './portal-account-card.component.html',
  styleUrl: './portal-account-card.component.scss',
})
export class PortalAccountCardComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly members = inject(TenantMembersApi);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly session = inject(WmsSession);

  /** `counterparties` yoki `agents` — WMS yo'lining bo'lagi. */
  readonly resource = input.required<'counterparties' | 'agents'>();
  readonly entityId = input.required<string>();
  readonly name = input.required<string>();
  readonly phone = input<string | null>(null);

  /** Identity roli: kontragentga `client`, agentga `agent`. */
  readonly role = input.required<'client' | 'agent'>();

  readonly account = signal<PortalAccount | null>(null);
  readonly busy = signal(false);
  readonly credentials = signal<TemporaryPasswordCredentials | null>(null);

  readonly enabled = computed(() => this.account()?.enabled ?? false);

  /** Hisob ochish — Identity yuzasi, u faqat `admin` rolini tan oladi. */
  readonly canManage = computed(() => this.session.hasRole('admin'));

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.api
      .get<PortalAccount>(`${this.resource()}/${this.entityId()}/portal-account`, undefined, {
        skipErrorNotify: true,
      })
      .subscribe({
        next: (res) => this.account.set(res.data ?? null),
        error: () => this.account.set(null),
      });
  }

  async open(): Promise<void> {
    const phone = this.phone()?.trim();
    if (!phone) {
      this.notify.warn(this.language.translate('portal.accountPhoneRequired'));
      return;
    }

    this.busy.set(true);
    try {
      const created = await this.members.addMember({
        phone: normalizePhone(phone),
        fullName: this.name(),
        role: this.role(),
      });

      this.link(created.member.userId, created.temporaryPassword);
    } catch (error) {
      this.busy.set(false);
      this.notify.error(
        error instanceof TenantApiError ? error.message : this.language.translate('errors.unknown')
      );
    }
  }

  private link(identitySub: string, temporaryPassword: string | null): void {
    this.api
      .put<PortalAccount>(`${this.resource()}/${this.entityId()}/portal-account`, { identitySub })
      .subscribe({
        next: (res) => {
          this.busy.set(false);
          this.account.set(res.data ?? null);
          if (temporaryPassword) {
            this.credentials.set({
              temporaryPassword,
              phone: this.phone(),
              fullName: this.name(),
            });
          } else {
            // Hisob avvaldan bor edi (odam boshqa mahsulotda ham ishlaydi) — parol o'zgarmadi.
            this.notify.success(this.language.translate('portal.accountOpened'));
          }
        },
        error: () => this.busy.set(false),
      });
  }

  /** Tasdiq dialogi — ilovaning umumiy `confirmDelete` i (qobiqda bitta `p-confirmDialog`). */
  revoke(): void {
    this.notify.confirmDelete(`${this.language.translate('portal.accountRevoke')}: ${this.name()}?`, () => {
      this.busy.set(true);
      this.api.delete<PortalAccount>(`${this.resource()}/${this.entityId()}/portal-account`).subscribe({
        next: (res) => {
          this.busy.set(false);
          this.account.set(res.data ?? null);
        },
        error: () => this.busy.set(false),
      });
    });
  }

  dismiss(): void {
    this.credentials.set(null);
  }
}
