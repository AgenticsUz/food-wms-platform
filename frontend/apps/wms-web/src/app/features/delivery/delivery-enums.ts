import { DeliveryStatus, DeliveryStopStatus } from './delivery.model';

/** Holat → `app-status-badge` CSS kaliti (eski `shared/utils/delivery-enums.ts`). */
export function deliveryStatusClass(status: DeliveryStatus): string {
  switch (status) {
    case DeliveryStatus.Planned:
      return 'Pending';
    case DeliveryStatus.InProgress:
      return 'InProgress';
    case DeliveryStatus.Completed:
      return 'Confirmed';
    case DeliveryStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Neutral';
  }
}

export function deliveryStatusKey(status: DeliveryStatus): string {
  switch (status) {
    case DeliveryStatus.InProgress:
      return 'delivery.statusInProgress';
    case DeliveryStatus.Completed:
      return 'delivery.statusCompleted';
    case DeliveryStatus.Cancelled:
      return 'delivery.statusCancelled';
    default:
      return 'delivery.statusPlanned';
  }
}

export function stopStatusClass(status: DeliveryStopStatus): string {
  switch (status) {
    case DeliveryStopStatus.Delivered:
      return 'Confirmed';
    case DeliveryStopStatus.Failed:
      return 'Cancelled';
    default:
      return 'Pending';
  }
}

export function stopStatusKey(status: DeliveryStopStatus): string {
  switch (status) {
    case DeliveryStopStatus.Delivered:
      return 'delivery.stopDelivered';
    case DeliveryStopStatus.Failed:
      return 'delivery.stopFailed';
    default:
      return 'delivery.stopPending';
  }
}
