namespace EnglishAdventure.Api.Contracts.Family;

/// <param name="Nickname">1 to 50 characters; unique among the family's active profiles (any letter case).</param>
/// <param name="AvatarKey">One of the keys from <c>GET api/v1/family/children/avatars</c>.</param>
public sealed record ChildProfileRequest(string Nickname, string AvatarKey);

/// <param name="MaxActive">How many active profiles a Parent may have.</param>
public sealed record ChildProfileOptionsResponse(IReadOnlyList<string> AvatarKeys, int MaxActive);
