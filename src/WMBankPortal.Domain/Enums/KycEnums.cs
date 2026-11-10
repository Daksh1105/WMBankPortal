namespace WMBankPortal.Domain.Enums;

public enum KycDocumentType
{
    IdProof = 0,
    AddressProof = 1,
    Photo = 2
}

public enum KycDocumentStatus
{
    Pending = 0,
    Verified = 1,
    Rejected = 2
}

public enum MalwareScanStatus
{
    Pending = 0,
    Clean = 1,
    Infected = 2,
    Failed = 3
}
