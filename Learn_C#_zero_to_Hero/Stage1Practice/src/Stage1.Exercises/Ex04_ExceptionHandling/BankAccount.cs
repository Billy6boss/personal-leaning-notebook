namespace Stage1.Exercises.Ex04_ExceptionHandling;

/// <summary>
/// Stage 1 主題：Exception handling（例外處理）。
///
/// 下面這三個標準 constructor 遵循一般 .NET exception 的慣例，
/// 已經幫你寫好了 — 真正的練習題是下面的 BankAccount，
/// 由它來決定何時該拋出這個 exception。
/// </summary>
public class InsufficientFundsException : Exception
{
    public InsufficientFundsException()
    {
    }

    public InsufficientFundsException(string message) : base(message)
    {
    }

    public InsufficientFundsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// BankAccount 的需求：
/// - Constructor：BankAccount(decimal initialBalance)。如果 initialBalance
///   是負數，要拋出 ArgumentOutOfRangeException。
/// - Balance（decimal）可以從外部讀取。
/// - Deposit(decimal amount)：如果 amount &lt;= 0，要拋出
///   ArgumentOutOfRangeException；否則把它加到 Balance。
/// - Withdraw(decimal amount)：如果 amount &lt;= 0，要拋出
///   ArgumentOutOfRangeException；如果 amount &gt; Balance，要拋出
///   InsufficientFundsException（附上有意義的訊息），並且 Balance
///   維持不變；否則把它從 Balance 扣除。
/// - 在你自己的實作中，於合理的地方適當地使用 try/catch/finally
///   （例如你有加上任何 logging 或 cleanup 邏輯的話）—
///   不要讓每個例外都不經檢視地直接往外拋。
/// </summary>
public class BankAccount
{
    public BankAccount(decimal initialBalance)
    {
        throw new NotImplementedException();
    }

    public decimal Balance => throw new NotImplementedException();

    public void Deposit(decimal amount)
    {
        throw new NotImplementedException();
    }

    public void Withdraw(decimal amount)
    {
        throw new NotImplementedException();
    }
}
