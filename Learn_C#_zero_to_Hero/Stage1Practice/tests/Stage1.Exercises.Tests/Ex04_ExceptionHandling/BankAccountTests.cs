using Stage1.Exercises.Ex04_ExceptionHandling;

namespace Stage1.Exercises.Tests.Ex04_ExceptionHandling;

public class InsufficientFundsExceptionTests
{
    [Fact]
    public void ParameterlessConstructor_Works()
    {
        var ex = new InsufficientFundsException();
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new InsufficientFundsException("not enough money");
        Assert.Equal("not enough money", ex.Message);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_SetsBoth()
    {
        var inner = new InvalidOperationException("root cause");
        var ex = new InsufficientFundsException("not enough money", inner);

        Assert.Equal("not enough money", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}

public class BankAccountTests
{
    [Fact]
    public void Constructor_WithNegativeInitialBalance_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BankAccount(-1m));
    }

    [Fact]
    public void Constructor_SetsInitialBalance()
    {
        var account = new BankAccount(100m);
        Assert.Equal(100m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Deposit_WithNonPositiveAmount_Throws(decimal amount)
    {
        var account = new BankAccount(100m);
        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount));
    }

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var account = new BankAccount(100m);
        account.Deposit(50m);
        Assert.Equal(150m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Withdraw_WithNonPositiveAmount_Throws(decimal amount)
    {
        var account = new BankAccount(100m);
        Assert.Throws<ArgumentOutOfRangeException>(() => account.Withdraw(amount));
    }

    [Fact]
    public void Withdraw_MoreThanBalance_ThrowsInsufficientFundsException_AndLeavesBalanceUnchanged()
    {
        var account = new BankAccount(100m);

        Assert.Throws<InsufficientFundsException>(() => account.Withdraw(1000m));
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void Withdraw_WithinBalance_DecreasesBalance()
    {
        var account = new BankAccount(100m);
        account.Withdraw(30m);
        Assert.Equal(70m, account.Balance);
    }
}
