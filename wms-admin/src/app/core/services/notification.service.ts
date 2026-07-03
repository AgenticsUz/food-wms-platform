import { Injectable, inject } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private msg = inject(MessageService);
  private confirm = inject(ConfirmationService);

  success(detail: string) { this.msg.add({ severity: 'success', summary: 'Success', detail }); }
  error(detail: string) { this.msg.add({ severity: 'error', summary: 'Error', detail }); }
  warn(detail: string) { this.msg.add({ severity: 'warn', summary: 'Warning', detail }); }

  confirmDelete(message: string, onAccept: () => void) {
    this.confirm.confirm({
      message, header: 'Confirm Delete', icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger', rejectButtonStyleClass: 'p-button-text', accept: onAccept
    });
  }

  confirmAction(message: string, header: string, onAccept: () => void) {
    this.confirm.confirm({ message, header, icon: 'pi pi-question-circle', accept: onAccept });
  }
}
