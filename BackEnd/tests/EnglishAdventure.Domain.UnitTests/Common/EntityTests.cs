using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.UnitTests.Common;

public sealed class EntityTests
{
    [Fact]
    public void Entities_of_the_same_type_with_the_same_id_are_equal()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new SampleEntity(id), new SampleEntity(id));
        Assert.True(new SampleEntity(id) == new SampleEntity(id));
        Assert.Equal(new SampleEntity(id).GetHashCode(), new SampleEntity(id).GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        Assert.NotEqual(new SampleEntity(Guid.NewGuid()), new SampleEntity(Guid.NewGuid()));
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        var id = Guid.NewGuid();

        Assert.False(new SampleEntity(id).Equals(new OtherEntity(id)));
    }

    [Fact]
    public void Entities_without_an_id_are_only_equal_to_themselves()
    {
        var entity = new SampleEntity(Guid.Empty);

        Assert.Equal(entity, entity);
        Assert.NotEqual(entity, new SampleEntity(Guid.Empty));
    }

    private sealed class SampleEntity : Entity
    {
        public SampleEntity(Guid id) => Id = id;
    }

    private sealed class OtherEntity : Entity
    {
        public OtherEntity(Guid id) => Id = id;
    }
}
