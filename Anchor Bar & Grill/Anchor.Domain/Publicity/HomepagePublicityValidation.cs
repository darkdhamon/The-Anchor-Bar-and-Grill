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
        else if (request.Headline.Trim().Length > HomepagePublicityConstraints.HeadlineMaxLength)
        {
            errors.Add($"The homepage headline must be {HomepagePublicityConstraints.HeadlineMaxLength} characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            errors.Add("A homepage summary is required.");
        }
        else if (request.Summary.Trim().Length > HomepagePublicityConstraints.SummaryMaxLength)
        {
            errors.Add($"The homepage summary must be {HomepagePublicityConstraints.SummaryMaxLength} characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(request.Eyebrow) && request.Eyebrow.Trim().Length > HomepagePublicityConstraints.EyebrowMaxLength)
        {
            errors.Add($"The homepage eyebrow must be {HomepagePublicityConstraints.EyebrowMaxLength} characters or fewer.");
        }

        return errors;
    }
}
