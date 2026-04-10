import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import { Transfer, TransferCreateDto } from '../models/transfer.model';

@Injectable({ providedIn: 'root' })
export class TransferService {
  private api = inject(ApiService);

  getTransfers(params?: Record<string, string | number | boolean>) {
    return this.api.get<Transfer[]>('transfers', params);
  }

  getTransfer(id: number) {
    return this.api.get<Transfer>(`transfers/${id}`);
  }

  createTransfer(dto: TransferCreateDto) {
    return this.api.post<Transfer>('transfers', dto);
  }

  confirmTransfer(id: number) {
    return this.api.put<Transfer>(`transfers/${id}/confirm`, {});
  }

  rejectTransfer(id: number) {
    return this.api.put<Transfer>(`transfers/${id}/reject`, {});
  }

  cancelTransfer(id: number) {
    return this.api.delete<void>(`transfers/${id}`);
  }
}
