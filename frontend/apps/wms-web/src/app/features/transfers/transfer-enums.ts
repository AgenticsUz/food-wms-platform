import { ReturnReason, TransferStatus, TransferType } from './transfer.model';

/**
 * Transfer enum → UI yordamchilari (eski `shared/utils/transfer-enums.ts`) —
 * bitta manba: bosh sahifa, harakatlar va transfer ekranlari shuni ishlatadi.
 *  - `*Class` — `StatusBadgeComponent` ning CSS kaliti;
 *  - `*Key`   — tarjima kaliti.
 */

export function transferStatusClass(status: TransferStatus): string {
  switch (status) {
    case TransferStatus.Pending:
      return 'Pending';
    case TransferStatus.Confirmed:
      return 'Confirmed';
    case TransferStatus.Rejected:
      return 'Rejected';
    case TransferStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Neutral';
  }
}

export function transferStatusKey(status: TransferStatus): string {
  switch (status) {
    case TransferStatus.Pending:
      return 'status.pending';
    case TransferStatus.Confirmed:
      return 'status.confirmed';
    case TransferStatus.Rejected:
      return 'status.rejected';
    case TransferStatus.Cancelled:
      return 'status.cancelled';
    default:
      return 'status.ok';
  }
}

export function transferTypeClass(type: TransferType): string {
  switch (type) {
    case TransferType.Incoming:
      return 'Incoming';
    case TransferType.Outgoing:
      return 'Outgoing';
    case TransferType.Internal:
      return 'Internal';
    case TransferType.ProductionOutput:
      return 'Production';
    case TransferType.Return:
      return 'Return';
    default:
      return 'Neutral';
  }
}

export function transferTypeKey(type: TransferType): string {
  switch (type) {
    case TransferType.Incoming:
      return 'transfer.incoming';
    case TransferType.Outgoing:
      return 'transfer.outgoing';
    case TransferType.Internal:
      return 'transfer.internal';
    case TransferType.ProductionOutput:
      return 'transfer.production';
    case TransferType.Return:
      return 'transfer.return';
    default:
      return 'transfer.transfers';
  }
}

export function returnReasonKey(reason: ReturnReason): string {
  switch (reason) {
    case ReturnReason.Expired:
      return 'transfer.expired';
    case ReturnReason.Unsold:
      return 'transfer.unsold';
    case ReturnReason.Defective:
      return 'transfer.defective';
    default:
      return 'transfer.other';
  }
}

/**
 * Transferning ko'rinadigan «raqami». Backendda qisqa raqam yo'q (id — Guid);
 * eski UI `#12` ko'rsatgan joyda Guid'ning boshlang'ich 8 belgisi chiqadi —
 * jadvalga sig'adi va operator uni ovoz chiqarib aytib bera oladi.
 */
export function shortTransferId(id: string | null | undefined): string {
  // Guid v7 ning boshi — vaqt belgisi (bir vaqtdagi yozuvlarda bir xil); farqlovchi qism — oxiri.
  return id ? id.slice(-8) : '';
}
