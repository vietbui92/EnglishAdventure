using MediatR;

namespace EnglishAdventure.Application.Foundation.Queries.GetFoundationStatus;

public sealed class GetFoundationStatusQueryHandler
    : IRequestHandler<GetFoundationStatusQuery, GetFoundationStatusResponse>
{
    public Task<GetFoundationStatusResponse> Handle(
        GetFoundationStatusQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = new GetFoundationStatusResponse(
            Message: "English Adventure API foundation is ready.",
            IsFoundationExample: true,
            Echo: request.Echo);

        return Task.FromResult(response);
    }
}
