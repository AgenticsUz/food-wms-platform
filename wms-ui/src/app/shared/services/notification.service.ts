import { Injectable, inject } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private msg = inject(MessageService);
  private confirm = inject(ConfirmationService);
  private transloco = inject(TranslocoService);

  success(message: string) {
    this.msg.add({ severity: 'success', summary: this.transloco.translate('common.success'), detail: message });
  }

  error(message: string) {
    this.msg.add({ severity: 'error', summary: this.transloco.translate('common.error'), detail: message });
  }

  warn(message: string) {
    this.msg.add({ severity: 'warn', summary: this.transloco.translate('common.warning'), detail: message });
  }

  info(message: string) {
    this.msg.add({ severity: 'info', summary: this.transloco.translate('common.info'), detail: message });
  }

  confirmDelete(message: string, onAccept: () => void) {
    this.confirm.confirm({
      message,
      header: this.transloco.translate('common.confirmDelete'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: onAccept
    });
  }

  confirmAction(message: string, header: string, onAccept: () => void) {
    this.confirm.confirm({
      message,
      header,
      icon: 'pi pi-question-circle',
      accept: onAccept
    });
  }
}
