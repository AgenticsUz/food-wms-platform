import { Injectable, inject } from '@angular/core';

import { ApiService, type QueryParams } from '../../core/api/api.service';
import type { LastPrice, Transfer, TransferCreateDto, TransferType } from './transfer.model';

/** Transferlar (eski `core/services/transfer.service`). */
@Injectable({ providedIn: 'root' })
export class TransferService {
  private readonly api = inject(ApiService);

  /**
   * Filtrlar: `type`, `status` (son), `from`/`to`, `counterpartyId`, `page`, `pageSize`.
   *
   * ⚠️ `from`/`to` — HUJJAT SANASI oralig'i (`DocumentDate`, P2.3), vaqt nuqtasi
   * EMAS: `YYYY-MM-DD` yuboriladi. Hujjat sanasi serverda kun boshi bo'lgani
   * uchun mahalliy kunni UTC lahzaga o'girish (`localDayRangeToUtc`) bu yerda
   * faqat chalkashtirardi — kun chegarasi ikkala tomonda ham kalendar kuni.
   */
  getTransfers(params?: QueryParams) {
    return this.api.get<Transfer[]>('transfers', params);
  }

  /**
   * Oxirgi tasdiqlangan hujjatdagi narx — forma narx maydonining taklifi.
   *
   * `Data = null` XATO EMAS: mahsulot birinchi marta kiritilayotgan bo'lsa
   * taklif shunchaki yo'q. Shuning uchun global spinner ham, xato toasti ham
   * o'chirilgan: yordamchi so'rov ekranni «yuklanmoqda» holatiga solmasin.
   */
  getLastPrice(productId: string, type: TransferType, counterpartyId: string | null) {
    return this.api.get<LastPrice | null>(
      'pricing/last-price',
      { productId, type, counterpartyId },
      { skipLoading: true, skipErrorNotify: true }
    );
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
