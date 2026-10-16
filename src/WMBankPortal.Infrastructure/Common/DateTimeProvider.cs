using WMBankPortal.Application.Common.Interfaces;

namespace WMBankPortal.Infrastructure.Common;

/// <summary>
/// Default implementation of IDateTimeProvider returning UTC wall clock time.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
