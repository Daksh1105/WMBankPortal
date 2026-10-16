namespace WMBankPortal.Domain.Exceptions;

/// <summary>
/// Thrown when an illegal state transition is attempted on an application or ticket.
/// Enforces business state machine invariants.
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string entityName, string fromState, string toState, string reason)
        : base($"Cannot transition {entityName} from state '{fromState}' to '{toState}'. Reason: {reason}")
    {
        EntityName = entityName;
        FromState = fromState;
        ToState = toState;
        Reason = reason;
    }

    public string EntityName { get; }
    public string FromState { get; }
    public string ToState { get; }
    public string Reason { get; }
}
