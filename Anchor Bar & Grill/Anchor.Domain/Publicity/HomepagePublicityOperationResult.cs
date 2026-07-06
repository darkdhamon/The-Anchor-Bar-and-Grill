namespace Anchor.Domain.Publicity;

public sealed record HomepagePublicityOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static HomepagePublicityOperationResult Success() => new(true, []);

    public static HomepagePublicityOperationResult Failure(params string[] errors) => new(false, errors);

    public static HomepagePublicityOperationResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
