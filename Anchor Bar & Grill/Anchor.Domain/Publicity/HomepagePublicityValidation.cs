namespace Anchor.Domain.Publicity;

internal static class HomepagePublicityValidation
{
    public static IReadOnlyList<string> Validate(SaveHomepagePublicityRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.Headline))
        {
            errors.Add("A homepage headline is required.");
        }
        else if (request.Headline.Trim().Length > 120)
        {
            errors.Add("The homepage headline must be 120 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            errors.Add("A homepage summary is required.");
        }
        else if (request.Summary.Trim().Length > 1000)
        {
            errors.Add("The homepage summary must be 1000 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(request.Eyebrow) && request.Eyebrow.Trim().Length > 80)
        {
            errors.Add("The homepage eyebrow must be 80 characters or fewer.");
        }

        return errors;
    }
}
