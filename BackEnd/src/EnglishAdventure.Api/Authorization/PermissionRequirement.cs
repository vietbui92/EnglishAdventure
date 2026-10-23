using Microsoft.AspNetCore.Authorization;

namespace EnglishAdventure.Api.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
