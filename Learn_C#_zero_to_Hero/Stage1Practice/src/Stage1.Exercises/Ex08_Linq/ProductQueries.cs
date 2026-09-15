namespace Stage1.Exercises.Ex08_Linq;

public record Product(string Name, string Category, decimal Price, int Stock);

/// <summary>
/// Stage 1 主題：LINQ。
///
/// 請用 LINQ 的 method-syntax（Where、Select、OrderBy、GroupBy、Sum 等）
/// 實作以下三個方法 — 核心邏輯請避免手寫 foreach 迴圈。
/// </summary>
public static class ProductQueries
{
    /// <summary>
    /// 回傳 Stock 恰好為 0 的所有 products。
    /// </summary>
    public static IEnumerable<Product> GetOutOfStock(IEnumerable<Product> products)
    {
        return products.Where(p => p.Stock == 0);
    }

    /// <summary>
    /// 回傳最貴的 <paramref name="n"/> 個 products 的名稱，
    /// 依價格由高到低排序。如果 products 數量少於 n 個，
    /// 就用相同的排序方式回傳全部的名稱。
    /// </summary>
    public static IEnumerable<string> GetTopNMostExpensiveNames(IEnumerable<Product> products, int n)
    {
        var expensiveP = products.OrderByDescending(p => p.Price).Select(p => p.Name);
        if (expensiveP.Count() < n)
        {
            return expensiveP;
        }

        return expensiveP.Take(1);
    }

    /// <summary>
    /// 針對每個不同的 Category，回傳該分類底下所有 products 的
    /// 庫存總價值（Price * Stock 加總）。
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> GetTotalInventoryValueByCategory(IEnumerable<Product> products)
    {
        var groupTotalPrice = products.GroupBy(p => p.Category)
            .ToDictionary(k => k.Key, v => v.Sum(p => p.Price * p.Stock));

        return groupTotalPrice;
    }
}
