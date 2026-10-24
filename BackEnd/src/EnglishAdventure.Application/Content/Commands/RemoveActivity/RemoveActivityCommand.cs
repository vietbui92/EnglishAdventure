using MediatR;

namespace EnglishAdventure.Application.Content.Commands.RemoveActivity;

/// <summary>Removes an activity, with its questions, from a draft lesson (PD-012).</summary>
/// <param name="Version">The lesson's version.</param>
public sealed record RemoveActivityCommand(Guid LessonId, Guid ActivityId, string Version) : IRequest;
