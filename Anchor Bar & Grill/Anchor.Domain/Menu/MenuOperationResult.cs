namespace Anchor.Domain.Menu;

public sealed record MenuOperationResult(bool Succeeded, IReadOnlyList<string> Errors, Guid? EntityId = null)
{
    public static MenuOperationResult Success(Guid? entityId = null) => new(true, Array.Empty<string>(), entityId);

    public static MenuOperationResult Failure(params string[] errors) => new(false, errors, null);

    public static MenuOperationResult Failure(IEnumerable<string> errors) => new(false, errors.ToArray(), null);
}
