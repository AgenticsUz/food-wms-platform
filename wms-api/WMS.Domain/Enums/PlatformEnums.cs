namespace WMS.Domain.Enums;

/// How a tenant paid for its subscription period. Platform-level (manual billing) —
/// unrelated to <see cref="PaymentMethod"/>, which records a tenant's own customer payments.
public enum PlatformPaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Other = 4
}

/// Why a tenant is suspended. Drives the refusal code the client receives, so support
/// can tell "you did not pay" from "you asked us to pause" from "we are doing maintenance".
public enum SuspendReason
{
    NonPayment = 1,
    ClientRequest = 2,
    Technical = 3,
    Violation = 4,
    Other = 5
}

/// Where a sales lead came from.
public enum LeadSource
{
    Website = 1,
    Portal = 2,
    Manual = 3,
    Referral = 4
}

/// Sales pipeline stage of a lead.
public enum LeadStatus
{
    New = 1,
    Contacted = 2,
    DemoGiven = 3,
    Won = 4,
    Lost = 5
}
