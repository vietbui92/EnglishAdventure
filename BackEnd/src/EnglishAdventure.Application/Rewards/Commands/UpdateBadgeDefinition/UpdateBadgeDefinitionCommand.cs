using EnglishAdventure.Application.Rewards.Models;
using MediatR;

namespace EnglishAdventure.Application.Rewards.Commands.UpdateBadgeDefinition;

/// <summary>
/// Changes a badge's name, description and picture (S5.2, owner 2026-10-24). Its rule cannot change. Children who won
/// it see the new texts.
/// </summary>
/// <param name="Kind">"FirstLesson", "Perfect", "LevelConqueror" or "LevelSuperstar".</param>
/// <param name="ImageUrl">An https picture; blank to let the app draw the badge.</param>
public sealed record UpdateBadgeDefinitionCommand(string Kind, string Name, string Description, string? ImageUrl)
    : IRequest<BadgeDefinitionView>;
