import { Injectable, inject } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private msg = inject(MessageService);
  private confirm = inject(ConfirmationService);

  success(message: string) {
    this.msg.add({ severity: 'success', summary: 'Success', detail: message });
  }

  error(message: string) {
    this.msg.add({ severity: 'error', summary: 'Error', detail: message });
  }

  warn(message: string) {
    this.msg.add({ severity: 'warn', summary: 'Warning', detail: message });
  }

  info(message: string) {
    this.msg.add({ severity: 'info', summary: 'Info', detail: message });
  }

  confirmDelete(message: string, onAccept: () => void) {
    this.confirm.confirm({
      message,
      header: 'Confirm Delete',
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
