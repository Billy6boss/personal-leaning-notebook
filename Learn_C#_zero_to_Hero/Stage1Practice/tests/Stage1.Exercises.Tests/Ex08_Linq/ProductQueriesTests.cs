using Stage1.Exercises.Ex08_Linq;

namespace Stage1.Exercises.Tests.Ex08_Linq;

public class ProductQueriesTests
{
    private static List<Product> SampleProducts() =>
        new()
        {
            new Product("Widget", "Tools", 9.99m, 10),
            new Product("Gadget", "Electronics", 199.99m, 0),
            new Product("Gizmo", "Electronics", 49.99m, 5),
            new Product("Bolt", "Tools", 0.5m, 100),
        };

    [Fact]
    public void GetOutOfStock_ReturnsOnlyZeroStockProducts()
    {
        var result = ProductQueries.GetOutOfStock(SampleProducts()).ToList();

        Assert.Single(result);
        Assert.Equal("Gadget", result[0].Name);
    }

    [Fact]
    public void GetTopNMostExpensiveNames_ReturnsOrderedByPriceDescending()
    {
        var result = ProductQueries.GetTopNMostExpensiveNames(SampleProducts(), 2).ToList();

        Assert.Equal(new[] { "Gadget", "Gizmo" }, result);
    }

    [Fact]
    public void GetTopNMostExpensiveNames_WhenNExceedsCount_ReturnsAllInOrder()
    {
        var result = ProductQueries.GetTopNMostExpensiveNames(SampleProducts(), 10).ToList();

        Assert.Equal(new[] { "Gadget", "Gizmo", "Widget", "Bolt" }, result);
    }

    [Fact]
    public void GetTotalInventoryValueByCategory_SumsPriceTimesStockPerCategory()
    {
        var result = ProductQueries.GetTotalInventoryValueByCategory(SampleProducts());

        Assert.Equal(149.9m, result["Tools"]);
        Assert.Equal(249.95m, result["Electronics"]);
    }
}
