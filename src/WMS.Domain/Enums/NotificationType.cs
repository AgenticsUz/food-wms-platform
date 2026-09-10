namespace WMS.Domain.Enums;

public enum NotificationType
{
    Info = 1,
    Warning = 2,
    LowStock = 3,
    TransferConfirmed = 4,
    TransferRejected = 5,
    Error = 6,
    BatchExpiring = 7,
    BatchExpired = 8,
    ProductionStarted = 9,
    ProductionCompleted = 10
}
