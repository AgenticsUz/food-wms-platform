import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal, type OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
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
  imports: [Button, InputText, PageHeaderComponent, TranslocoDirective, DatePipe],
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

  private pollSubscription: Subscription | null = null;

  ngOnInit(): void {
    this.loadStatus();
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
      if (res.success && res.data) this.telegram.set(res.data);
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
