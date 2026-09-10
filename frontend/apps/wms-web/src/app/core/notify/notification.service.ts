import { Injectable, inject } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { LanguageService } from '@agentics/i18n';

/**
 * Toast va tasdiqlash — eski `wms-ui` `NotificationService` i bilan BIR XIL API.
 *
 * Nega `@agentics/notify` ning `ToastService` i emas: u i18n KALITINI kutadi va
 * UI qatlami (toast host) mahsulotning o'zida yozilishi kerak. WMS ekranlari
 * esa PrimeNG `p-toast` ga TAYYOR matn uzatadi (ko'pincha serverdan kelgan,
 * allaqachon tarjima qilingan `message`). Ko'chirilgan ekran shu servisni
 * o'zgarishsiz chaqirishi uchun API saqlandi.
 *
 * Qoida o'zgarmadi: toast FAQAT shu servis orqali, `MessageService` to'g'ridan-
 * to'g'ri emas. Xato toastini `WmsErrorNotifier` allaqachon chiqaradi —
 * komponent ikkinchisini qo'shmaydi.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly messages = inject(MessageService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly language = inject(LanguageService);

  success(message: string): void {
    this.messages.add({ severity: 'success', summary: this.t('common.success'), detail: message });
  }

  error(message: string): void {
    this.messages.add({ severity: 'error', summary: this.t('common.error'), detail: message });
  }

  warn(message: string): void {
    this.messages.add({ severity: 'warn', summary: this.t('common.warning'), detail: message });
  }

  info(message: string): void {
    this.messages.add({ severity: 'info', summary: this.t('common.info'), detail: message });
  }

  confirmDelete(message: string, onAccept: () => void): void {
    this.confirmation.confirm({
      message,
      header: this.t('common.confirmDelete'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      acceptLabel: this.t('common.yes'),
      rejectLabel: this.t('common.no'),
      accept: onAccept,
    });
  }

  confirmAction(message: string, header: string, onAccept: () => void): void {
    this.confirmation.confirm({
      message,
      header,
      icon: 'pi pi-question-circle',
      acceptLabel: this.t('common.yes'),
      rejectLabel: this.t('common.no'),
      accept: onAccept,
    });
  }

  /** `LanguageService.translate` — kirill rejimida natija transliteratsiya qilinadi. */
  private t(key: string): string {
    return this.language.translate(key);
  }
}
