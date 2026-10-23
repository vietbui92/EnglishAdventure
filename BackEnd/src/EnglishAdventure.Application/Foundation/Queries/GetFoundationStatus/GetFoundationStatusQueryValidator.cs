using FluentValidation;

namespace EnglishAdventure.Application.Foundation.Queries.GetFoundationStatus;

public sealed class GetFoundationStatusQueryValidator : AbstractValidator<GetFoundationStatusQuery>
{
    public GetFoundationStatusQueryValidator()
    {
        RuleFor(query => query.Echo)
            .MaximumLength(50)
            .When(query => query.Echo is not null);
    }
}
