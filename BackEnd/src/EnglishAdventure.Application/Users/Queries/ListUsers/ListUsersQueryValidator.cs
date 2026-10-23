using FluentValidation;

namespace EnglishAdventure.Application.Users.Queries.ListUsers;

public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public const int MaximumPageSize = 100;

    public ListUsersQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, MaximumPageSize);
        RuleFor(query => query.Search).MaximumLength(256);
    }
}
