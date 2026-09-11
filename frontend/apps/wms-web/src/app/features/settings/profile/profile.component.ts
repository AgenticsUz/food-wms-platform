import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal, type OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Subscription, switchMap, takeWhile, timer } from 'rxjs';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { TelegramStatus } from '../settings.model';
import { SettingsService } from '../settings.service';

/** Ulanishni kutishda holat necha soniyada qayta so'raladi. */
const LINK_POLL_MS = 3000;

/**
 * O'chirgich guruhlari (TG3): tur nomlari backend `NotificationType` bilan bir xil.
 * Guruh faqat tegishli ruxsati borga ko'rsatiladi — server baribir shu ruxsat bo'yicha filtrlaydi.
 */
interface MuteGroup {
  readonly key: string;
  readonly permission: string;
  readonly types: readonly string[];
}

const MUTE_GROUPS: readonly MuteGroup[] = [
  { key: 'settings.telegramGroupWarehouse', permission: 'warehouse.view', types: ['LowStock', 'BatchExpiring', 'BatchExpired'] },
  { key: 'settings.telegramGroupTransfers', permission: 'transfers.view', types: ['TransferConfirmed', 'TransferRejected', 'ReturnReceived'] },
  { key: 'settings.telegramGroupProduction', permission: 'production.view', types: ['ProductionStarted', 'ProductionCompleted'] },
  { key: 'settings.telegramGroupSubscription', permission: 'settings.modules', types: ['SubscriptionWarning', 'SubscriptionSuspended', 'LimitWarning'] },
];

/**
 * Mening profilim (D5).
 *
 * O'CHGAN: parol almashtirish (va `mustChangePassword` majburiy rejimi) — parol
 * Agentics ID'da; ism/telefon tahriri — ular Identity'niki, JIT har tokenda
 * nusxani qayta yozadi, WMS'dagi tahrir bir necha daqiqadan keyin jimgina bekor
 * bo'lardi. Qolgani — WMS'ning o'z sozlamasi: Telegram bildirishnomalari.
 *
 * Telegram (TG1/TG7): qo'lda chat ID o'rniga deep-link — bot foydalanuvchiga u
 * `/start` bosmaguncha yoza olmaydi (403), shuning uchun eski «chat ID kiriting»
 * yo'li amalda ishlamasdi. Tugma havola oladi, Telegram'ni ochadi va bot ulaganini
 * kutib holatni qayta so'raydi.
 */
@Component({
  selector: 'app-profile',
  imports: [Button, Checkbox, FormsModule, InputText, PageHeaderComponent, TranslocoDirective, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export default class ProfileComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly session = inject(WmsSession);

  /** `null` — hali o'qilmagan. */
  readonly telegram = signal<TelegramStatus | null>(null);
  readonly linking = signal(false);
  readonly unlinking = signal(false);
  /** Havola berilgan va bot `/start` ni kutmoqda; `null` — kutish yo'q. */
  readonly pendingLink = signal<string | null>(null);
  readonly linkExpired = signal(false);

  /** O'chirilgan turlar — server holatidan; checkbox o'zgarganda darhol saqlanadi. */
  readonly muted = signal<readonly string[]>([]);
  readonly savingMuted = signal(false);
  /** Kunlik xulosa (TG11) — faqat `dashboard.view` bo'lganga ko'rsatiladi. */
  readonly digest = signal(false);

  private pollSubscription: Subscription | null = null;

  ngOnInit(): void {
    this.loadStatus();
  }

  /** Foydalanuvchining ruxsati bor guruhlar. */
  get muteGroups(): readonly MuteGroup[] {
    return MUTE_GROUPS.filter((g) => this.session.can(g.permission));
  }

  /** Guruh yoqiqmi — guruhdagi birorta tur ham o'chirilmagan bo'lsa. */
  isGroupOn(group: MuteGroup): boolean {
    const muted = this.muted();
    return !group.types.some((t) => muted.includes(t));
  }

  toggleDigest(on: boolean): void {
    this.digest.set(on);
    this.savingMuted.set(true);
    this.settingsService.setTelegramDigest(on).subscribe({
      next: () => this.savingMuted.set(false),
      error: () => {
        this.savingMuted.set(false);
        this.loadStatus();
      },
    });
  }

  toggleGroup(group: MuteGroup, on: boolean): void {
    const without = this.muted().filter((t) => !group.types.includes(t));
    const next = on ? without : [...without, ...group.types];
    this.muted.set(next);
    this.savingMuted.set(true);
    this.settingsService.setTelegramMuted(next).subscribe({
      next: () => this.savingMuted.set(false),
      error: () => {
        this.savingMuted.set(false);
        this.loadStatus();
      },
    });
  }

  connect(): void {
    this.linking.set(true);
    this.linkExpired.set(false);
    this.settingsService.createTelegramLink().subscribe({
      next: (res) => {
        this.linking.set(false);
        if (!res.success || !res.data) return;

        this.pendingLink.set(res.data.url);
        // `noopener`: t.me sahifasi bizning oynaga ega bo'lmasin. Bloklansa — havola matnda turadi.
        window.open(res.data.url, '_blank', 'noopener');
        this.waitForLink(new Date(res.data.expiresAt).getTime());
      },
      error: () => this.linking.set(false),
    });
  }

  disconnect(): void {
    this.unlinking.set(true);
    this.settingsService.unlinkTelegram().subscribe({
      next: () => {
        this.unlinking.set(false);
        this.notify.success(this.language.translate('settings.telegramDisconnected'));
        this.loadStatus();
      },
      error: () => this.unlinking.set(false),
    });
  }

  private loadStatus(): void {
    this.settingsService.getTelegram().subscribe((res) => {
      if (res.success && res.data) {
        this.telegram.set(res.data);
        this.muted.set(res.data.mutedTypes);
        this.digest.set(res.data.digest);
      }
    });
  }

  /**
   * Har 3 s holatni so'raydi — `linked` bo'lguncha yoki havola muddati tugaguncha.
   * Oyna yopilib qolsa ham cheksiz so'rov bo'lmaydi: `expiresAt` da to'xtaydi.
   */
  private waitForLink(expiresAtMs: number): void {
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = timer(LINK_POLL_MS, LINK_POLL_MS)
      .pipe(
        takeWhile(() => Date.now() < expiresAtMs),
        // Fon so'rovi: progress chizig'i miltillamasin, o'tkinchi xato toast bermasin.
        switchMap(() => this.settingsService.getTelegram({ skipLoading: true, skipErrorNotify: true })),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          if (!res.success || !res.data?.linked) return;
          this.telegram.set(res.data);
          this.muted.set(res.data.mutedTypes);
          this.digest.set(res.data.digest);
          this.pendingLink.set(null);
          this.pollSubscription?.unsubscribe();
          this.notify.success(this.language.translate('common.success'));
        },
        complete: () => {
          // Muddat tugadi, hali ulanmagan.
          if (this.pendingLink() !== null) {
            this.pendingLink.set(null);
            this.linkExpired.set(true);
          }
        },
      });
  }
}
