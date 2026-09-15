using Stage1.Exercises.Ex06_Generics;

namespace Stage1.Exercises.Tests.Ex06_Generics;

public class RepositoryTests
{
    private sealed class TestEntity : IHasId
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }

    [Fact]
    public void Add_ThenGetById_ReturnsTheItem()
    {
        var repo = new Repository<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "First" };

        repo.Add(entity);

        Assert.Same(entity, repo.GetById(1));
    }

    [Fact]
    public void Add_WithDuplicateId_Throws()
    {
        var repo = new Repository<TestEntity>();
        repo.Add(new TestEntity { Id = 1, Name = "First" });

        Assert.Throws<ArgumentException>(() => repo.Add(new TestEntity { Id = 1, Name = "Second" }));
    }

    [Fact]
    public void GetById_WhenMissing_ReturnsNull()
    {
        var repo = new Repository<TestEntity>();
        Assert.Null(repo.GetById(999));
    }

    [Fact]
    public void GetAll_ReturnsEveryStoredItem()
    {
        var repo = new Repository<TestEntity>();
        repo.Add(new TestEntity { Id = 1, Name = "First" });
        repo.Add(new TestEntity { Id = 2, Name = "Second" });

        var all = repo.GetAll();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, e => e.Id == 1);
        Assert.Contains(all, e => e.Id == 2);
    }

    [Fact]
    public void Remove_WhenPresent_RemovesAndReturnsTrue()
    {
        var repo = new Repository<TestEntity>();
        repo.Add(new TestEntity { Id = 1, Name = "First" });

        var removed = repo.Remove(1);

        Assert.True(removed);
        Assert.Null(repo.GetById(1));
    }

    [Fact]
    public void Remove_WhenMissing_ReturnsFalse()
    {
        var repo = new Repository<TestEntity>();
        Assert.False(repo.Remove(999));
    }
}
