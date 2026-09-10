import { Injectable, inject } from '@angular/core';

import { ApiService, type QueryParams } from '../../core/api/api.service';
import type { Transfer, TransferCreateDto } from './transfer.model';

/** Transferlar (eski `core/services/transfer.service`). */
@Injectable({ providedIn: 'root' })
export class TransferService {
  private readonly api = inject(ApiService);

  /**
   * Filtrlar: `type`, `status` (son), `from`/`to` (UTC vaqt nuqtasi — backend
   * `CreatedAt >= from` va `CreatedAt <= to` qiladi), `counterpartyId`, `page`, `pageSize`.
   */
  getTransfers(params?: QueryParams) {
    return this.api.get<Transfer[]>('transfers', params);
  }

  getTransfer(id: string) {
    return this.api.get<Transfer>(`transfers/${id}`);
  }

  createTransfer(dto: TransferCreateDto) {
    return this.api.post<Transfer>('transfers', dto);
  }

  confirmTransfer(id: string) {
    return this.api.put<Transfer>(`transfers/${id}/confirm`, {});
  }

  rejectTransfer(id: string) {
    return this.api.put<Transfer>(`transfers/${id}/reject`, {});
  }

  cancelTransfer(id: string) {
    return this.api.delete<null>(`transfers/${id}`);
  }
}
