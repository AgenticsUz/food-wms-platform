import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SettingsService } from '../settings.service';

/**
 * Mening profilim (D5).
 *
 * O'CHGAN: parol almashtirish (va `mustChangePassword` majburiy rejimi) — parol
 * Agentics ID'da; ism/telefon tahriri — ular Identity'niki, JIT har tokenda
 * nusxani qayta yozadi, WMS'dagi tahrir bir necha daqiqadan keyin jimgina bekor
 * bo'lardi. Qolgani — WMS'ning o'z sozlamasi: Telegram bildirishnomalari.
 */
@Component({
  selector: 'app-profile',
  imports: [FormsModule, Button, InputText, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export default class ProfileComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  protected readonly session = inject(WmsSession);

  readonly telegramChatId = signal('');
  /** Bot sozlanmagan bo'lsa ulash foydasiz — ogohlantiramiz (`null` — hali o'qilmagan). */
  readonly telegramEnabled = signal<boolean | null>(null);
  readonly savingTelegram = signal(false);

  ngOnInit(): void {
    // Eskisida chat ID login javobida kelardi; o'z login'i o'chdi (D5) — alohida so'rov.
    this.settingsService.getTelegram().subscribe((res) => {
      if (res.success && res.data) {
        this.telegramChatId.set(res.data.chatId ?? '');
        this.telegramEnabled.set(res.data.enabled);
      }
    });
  }

  saveTelegram(): void {
    this.savingTelegram.set(true);
    const chatId = this.telegramChatId().trim() || null;
    this.settingsService.setTelegram(chatId).subscribe({
      next: () => {
        this.savingTelegram.set(false);
        this.notify.success(this.language.translate('common.success'));
      },
      error: () => this.savingTelegram.set(false),
    });
  }
}
