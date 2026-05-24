namespace Anchor.Domain.Events;

public sealed record EventOperationResult(bool Succeeded, Guid? EventId, IReadOnlyList<string> Errors)
{
    public static EventOperationResult Success(Guid? eventId = null) => new(true, eventId, []);

    public static EventOperationResult Failure(params string[] errors) => new(false, null, errors);

    public static EventOperationResult Failure(IReadOnlyList<string> errors) => new(false, null, errors);
}
