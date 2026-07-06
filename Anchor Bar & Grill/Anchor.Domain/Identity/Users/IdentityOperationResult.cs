namespace Anchor.Domain.Identity.Users;

public sealed record IdentityOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static IdentityOperationResult Success() => new(true, Array.Empty<string>());

    public static IdentityOperationResult Failure(params string[] errors) => new(false, errors);

    public static IdentityOperationResult Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}
