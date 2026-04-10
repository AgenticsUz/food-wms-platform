export interface Transfer {
  id: number;
  type: TransferType;
  typeName?: string;
  fromWarehouseId: number | null;
  fromWarehouseName: string | null;
  toWarehouseId: number | null;
  toWarehouseName: string | null;
  counterpartyId: number | null;
  counterpartyName: string | null;
  createdByUserId: number | null;
  createdByUserName: string | null;
  status: TransferStatus;
  statusName?: string;
  note: string | null;
  confirmedAt: string | null;
  items: TransferItem[];
  totalAmount: number;
  createdAt: string;
}

export enum TransferType {
  Incoming = 1,
  Outgoing = 2,
  Internal = 3,
  ProductionOutput = 4
}

export enum TransferStatus {
  Pending = 1,
  Confirmed = 2,
  Rejected = 3,
  Cancelled = 4
}

export interface TransferItem {
  id?: number;
  transferId?: number;
  productId: number;
  productName?: string;
  batchId: number | null;
  lotNumber?: string;
  quantity: number;
  unitPrice: number;
  totalPrice?: number;
}

export interface TransferCreateDto {
  type: TransferType;
  fromWarehouseId: number | null;
  toWarehouseId: number | null;
  counterpartyId: number | null;
  note: string | null;
  items: TransferItemDto[];
}

export interface TransferItemDto {
  productId: number;
  batchId: number | null;
  quantity: number;
  unitPrice: number;
}
