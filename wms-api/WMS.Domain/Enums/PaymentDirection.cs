namespace WMS.Domain.Enums;

/// In — money received from the counterparty (their debt to us decreases).
/// Out — money paid to the counterparty (our debt to them decreases).
public enum PaymentDirection { In = 1, Out = 2 }
