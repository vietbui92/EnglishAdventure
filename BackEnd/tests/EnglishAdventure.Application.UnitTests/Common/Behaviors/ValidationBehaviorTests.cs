using EnglishAdventure.Application.Common.Behaviors;
using EnglishAdventure.Application.Foundation.Queries.GetFoundationStatus;
using FluentValidation;
using MediatR;

namespace EnglishAdventure.Application.UnitTests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_throws_validation_exception_when_request_is_invalid()
    {
        var validator = new GetFoundationStatusQueryValidator();
        var behavior = new ValidationBehavior<GetFoundationStatusQuery, GetFoundationStatusResponse>(
            new IValidator<GetFoundationStatusQuery>[] { validator });

        var invalidQuery = new GetFoundationStatusQuery(Echo: new string('x', 51));

        var act = () => behavior.Handle(
            invalidQuery,
            _ => Task.FromResult(new GetFoundationStatusResponse("ok", true, invalidQuery.Echo)),
            CancellationToken.None);

        await Assert.ThrowsAsync<ValidationException>(act);
    }

    [Fact]
    public async Task Handle_invokes_next_when_request_is_valid()
    {
        var validator = new GetFoundationStatusQueryValidator();
        var behavior = new ValidationBehavior<GetFoundationStatusQuery, GetFoundationStatusResponse>(
            new IValidator<GetFoundationStatusQuery>[] { validator });

        var expected = new GetFoundationStatusResponse("ok", true, "hello");
        var invoked = false;

        var result = await behavior.Handle(
            new GetFoundationStatusQuery("hello"),
            _ =>
            {
                invoked = true;
                return Task.FromResult(expected);
            },
            CancellationToken.None);

        Assert.True(invoked);
        Assert.Equal(expected, result);
    }
}
