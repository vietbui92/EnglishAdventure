using EnglishAdventure.Domain.Entities.Rewards;
using EnglishAdventure.Domain.Enums;
using EnglishAdventure.Domain.Exceptions;
using EnglishAdventure.Domain.Services;
using EnglishAdventure.Domain.ValueObjects;

namespace EnglishAdventure.Domain.UnitTests.Services;

public sealed class RewardRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);

    private readonly Guid _level = Guid.NewGuid();
    private readonly Guid _pets = Guid.NewGuid();
    private readonly Guid _farm = Guid.NewGuid();
    private readonly Guid _lion = Guid.NewGuid();

    private IReadOnlyList<LevelPath> Map => [new LevelPath(_level, [_pets, _farm], _lion)];

    private static Dictionary<Guid, int> Stars(params (Guid Lesson, int Stars)[] best) =>
        best.ToDictionary(entry => entry.Lesson, entry => entry.Stars);

    private static HashSet<BadgeWin> Owned(params BadgeWin[] badges) => badges.ToHashSet();

    [Fact]
    public void Each_newly_earned_star_is_worth_ten_points()
    {
        var first = RewardRules.For(_pets, 0, Map, Stars((_pets, 2)), Owned());
        var better = RewardRules.For(_pets, 2, Map, Stars((_pets, 3)), Owned());
        var same = RewardRules.For(_pets, 3, Map, Stars((_pets, 3)), Owned());

        Assert.Equal([1, 2], first.NewStars);
        Assert.Equal(20, first.Points);
        Assert.Equal([3], better.NewStars);
        Assert.Equal(10, better.Points);
        Assert.Empty(same.NewStars);
    }

    [Fact]
    public void The_first_completion_and_the_first_three_stars_win_their_badges_once()
    {
        var first = RewardRules.For(_pets, 0, Map, Stars((_pets, 3)), Owned());
        var later = RewardRules.For(
            _farm,
            0,
            Map,
            Stars((_pets, 3), (_farm, 3)),
            Owned(new BadgeWin(BadgeKind.FirstLesson, null), new BadgeWin(BadgeKind.Perfect, null)));

        Assert.Equal(
            [new BadgeWin(BadgeKind.FirstLesson, null), new BadgeWin(BadgeKind.Perfect, null)],
            first.Badges);
        Assert.Empty(later.Badges);
    }

    [Fact]
    public void Defeating_the_boss_conquers_the_level_and_three_stars_everywhere_make_a_superstar()
    {
        var owned = Owned(new BadgeWin(BadgeKind.FirstLesson, null), new BadgeWin(BadgeKind.Perfect, null));

        var conquered = RewardRules.For(_lion, 0, Map, Stars((_pets, 3), (_farm, 2), (_lion, 3)), owned);
        var superstar = RewardRules.For(
            _farm,
            2,
            Map,
            Stars((_pets, 3), (_farm, 3), (_lion, 3)),
            owned.Append(new BadgeWin(BadgeKind.LevelConqueror, _level)).ToHashSet());

        Assert.Equal([new BadgeWin(BadgeKind.LevelConqueror, _level)], conquered.Badges);
        Assert.Equal([new BadgeWin(BadgeKind.LevelSuperstar, _level)], superstar.Badges);
    }

    [Fact]
    public void A_level_without_a_boss_has_no_level_badges()
    {
        IReadOnlyList<LevelPath> map = [new LevelPath(_level, [_pets], BossId: null)];

        var result = RewardRules.For(_pets, 0, map, Stars((_pets, 3)), Owned());

        Assert.DoesNotContain(result.Badges, badge => badge.LevelId is not null);
    }

    [Fact]
    public void Badges_have_editable_texts_naming_their_level()
    {
        var badge = BadgeDefinition.Create(BadgeKind.LevelConqueror, "Chinh phục {level}", "Đánh bại boss của {level}.", null);

        badge.Change(" Vua {level} ", "Thắng boss {level}.", ImageUrl.From("https://cdn.example.test/crown.png"));

        Assert.True(badge.PerLevel);
        Assert.Equal("Vua {level}", badge.Name);
        Assert.Equal("Vua Đảo Thú Cưng", BadgeDefinition.For(badge.Name, "Đảo Thú Cưng"));
        Assert.Equal("https://cdn.example.test/crown.png", badge.ImageUrl!.Value);
        Assert.Throws<DomainException>(() => badge.Change(" ", "x", null));
        Assert.Throws<DomainException>(() => badge.Change(new string('a', 61), "x", null));
        Assert.False(BadgeDefinition.Create(BadgeKind.Perfect, "Hoàn hảo", "3 sao.", null).PerLevel);
    }

    [Fact]
    public void Points_and_awards_check_what_they_belong_to()
    {
        var points = PointsTransaction.ForStar(Guid.NewGuid(), _pets, 2, Now);

        Assert.Equal(10, points.Points);
        Assert.Throws<DomainException>(() => PointsTransaction.ForStar(Guid.NewGuid(), _pets, 4, Now));
        Assert.Throws<DomainException>(() => BadgeAward.Win(Guid.NewGuid(), BadgeKind.LevelConqueror, null, _lion, Now));
        Assert.Throws<DomainException>(() => BadgeAward.Win(Guid.NewGuid(), BadgeKind.Perfect, _level, _pets, Now));
        Assert.Equal(_level, BadgeAward.Win(Guid.NewGuid(), BadgeKind.LevelSuperstar, _level, _lion, Now).LevelId);
    }
}
