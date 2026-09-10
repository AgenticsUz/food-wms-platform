namespace WMS.Domain.Enums;

public enum CommissionStatus
{
    Pending = 1,    // sale confirmed, client has not paid yet (on credit)
    Confirmed = 2,  // client paid — commission is real/earned
    Cancelled = 3   // goods returned / sale reversed — no commission
}
