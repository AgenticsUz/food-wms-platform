import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { ToggleSwitch } from 'primeng/toggleswitch';

import { WMS_MODULES } from '../../../core/auth/wms-me.model';
import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { TelegramClientSettings } from '../settings.model';
import { SettingsService } from '../settings.service';

/**
 * Faqat-ko'rish sahifa (D6). Modullar Agentics orqali sotiladi va Identity'dagi
 * tenant obunasida yoqiladi — WMS'da o'chirgich yo'q. Manba `/api/me`
 * (`WmsSession.modules`), alohida so'rov kerak emas: menyu ham shu ro'yxatga qaraydi,
 * ikki manba bo'lsa bir-biridan ajralib qolardi.
 */
@Component({
  selector: 'app-modules',
  imports: [RouterLink, PageHeaderComponent, TranslocoDirective, FormsModule, Button, InputNumber, ToggleSwitch],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './modules.component.html',
  styleUrl: './modules.component.scss',
})
export default class ModulesComponent implements OnInit {
  private readonly session = inject(WmsSession);
  private readonly settingsService = inject(SettingsService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  /** Mijozlarga Telegram xabarlari (TG13) — tenant egasining roziligi; sukut o'chiq. */
  readonly canManage = computed(() => this.session.can('settings.modules'));
  readonly clientSettings = signal<TelegramClientSettings | null>(null);
  readonly savingClients = signal(false);

  ngOnInit(): void {
    if (!this.canManage()) return;
    this.settingsService.getTelegramClients().subscribe((res) => {
      if (res.success && res.data) this.clientSettings.set(res.data);
    });
  }

  updateClients(patch: Partial<TelegramClientSettings>): void {
    const current = this.clientSettings();
    if (current) this.clientSettings.set({ ...current, ...patch });
  }

  saveClients(): void {
    const dto = this.clientSettings();
    if (!dto) return;
    this.savingClients.set(true);
    this.settingsService.setTelegramClients(dto).subscribe({
      next: () => {
        this.savingClients.set(false);
        this.notify.success(this.language.translate('common.success'));
      },
      error: () => this.savingClients.set(false),
    });
  }

  /** Hamma 11 modul — yoqilganmi yoki yo'qmi; o'chiqlari ham ko'rinadi (yumshoq upsell). */
  readonly modules = computed(() =>
    WMS_MODULES.map((code) => ({ code, isEnabled: this.session.modules().has(code) }))
  );
}
