namespace AMS.Core.Enums;

/// <summary>
/// Types of insurance policies
/// </summary>
public enum PolicyType
{
    Auto = 1,
    Home = 2,
    Life = 3,
    Health = 4,
    Business = 5
}

/// <summary>
/// Status of a policy
/// </summary>
public enum PolicyStatus
{
    Pending = 1,
    Active = 2,
    Expired = 3,
    Cancelled = 4
}

/// <summary>
/// Status of a claim
/// </summary>
public enum ClaimStatus
{
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Paid = 5
}
