using Stage1.Exercises.Ex02_TypesAndMembers;

namespace Stage1.Exercises.Tests.Ex02_TypesAndMembers;

public class MoneyTests
{
    [Fact]
    public void ConstructsWithAmountAndCurrencyCode()
    {
        var money = new Money(10.5m, "USD");

        Assert.Equal(10.5m, money.Amount);
        Assert.Equal("USD", money.CurrencyCode);
    }

    [Fact]
    public void TwoInstancesWithSameAmountAndCurrency_AreEqual()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "USD");

        Assert.Equal(a, b);
    }

    [Fact]
    public void InstancesWithDifferentCurrency_AreNotEqual()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "TWD");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void InstancesWithDifferentAmount_AreNotEqual()
    {
        var a = new Money(10m, "USD");
        var b = new Money(20m, "USD");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Add_WithSameCurrency_SumsAmounts()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5.5m, "USD");

        var result = a.Add(b);

        Assert.Equal(15.5m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public void Add_WithDifferentCurrency_Throws()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5m, "TWD");

        Assert.Throws<InvalidOperationException>(() => a.Add(b));
    }
}
