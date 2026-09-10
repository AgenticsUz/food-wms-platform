import { ProductionOrderStatus, StageExecutionStatus } from './production.model';

/**
 * Holat → `StatusBadgeComponent` CSS kaliti va tarjima kaliti. Eski ekranlarda
 * `getStatusName()` inglizcha matn qaytarardi; endi kalit qaytariladi va shablon
 * `t(...)` bilan tarjima qiladi — til almashganda yorliq ham almashadi.
 */

export function orderStatusClass(status: ProductionOrderStatus): string {
  switch (status) {
    case ProductionOrderStatus.Draft:
      return 'Draft';
    case ProductionOrderStatus.InProgress:
      return 'InProgress';
    case ProductionOrderStatus.Completed:
      return 'Completed';
    case ProductionOrderStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Neutral';
  }
}

export function orderStatusKey(status: ProductionOrderStatus): string {
  switch (status) {
    case ProductionOrderStatus.Draft:
      return 'status.draft';
    case ProductionOrderStatus.InProgress:
      return 'status.inProgress';
    case ProductionOrderStatus.Completed:
      return 'status.completed';
    case ProductionOrderStatus.Cancelled:
      return 'status.cancelled';
    default:
      return 'status.ok';
  }
}

export function stageStatusClass(status: StageExecutionStatus): string {
  switch (status) {
    case StageExecutionStatus.Pending:
      return 'Pending';
    case StageExecutionStatus.InProgress:
      return 'InProgress';
    case StageExecutionStatus.Completed:
      return 'Completed';
    case StageExecutionStatus.Skipped:
      return 'Cancelled';
    default:
      return 'Neutral';
  }
}

export function stageStatusKey(status: StageExecutionStatus): string {
  switch (status) {
    case StageExecutionStatus.Pending:
      return 'status.pending';
    case StageExecutionStatus.InProgress:
      return 'status.inProgress';
    case StageExecutionStatus.Completed:
      return 'status.completed';
    case StageExecutionStatus.Skipped:
      return 'production.skipped';
    default:
      return 'status.ok';
  }
}
