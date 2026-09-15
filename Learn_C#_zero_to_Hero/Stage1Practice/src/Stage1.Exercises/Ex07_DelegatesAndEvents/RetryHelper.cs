namespace Stage1.Exercises.Ex07_DelegatesAndEvents;

/// <summary>
/// Stage 1 主題：Delegates 與 events。
///
/// 任務：
/// 實作 Execute，讓它呼叫 <paramref name="operation"/>。如果過程中
/// 拋出任何 Exception，就重試再呼叫一次，最多總共呼叫
/// <paramref name="maxAttempts"/> 次（所以 maxAttempts = 3 代表總共
/// 最多呼叫 3 次，而不是第一次之後再重試 3 次）。只要有一次呼叫成功，
/// 就立刻回傳它的結果。如果每次嘗試都拋出例外，讓「最後一次」嘗試的
/// 例外往外傳播（propagate）出 Execute（不要吞掉它）。如果
/// maxAttempts &lt;= 0，要拋出 ArgumentOutOfRangeException。
/// </summary>
public static class RetryHelper
{
    public static T Execute<T>(Func<T> operation, int maxAttempts)
    {
        if (maxAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "smaller  than 0");
        }

        T? result = default;
        while (maxAttempts >= 0)
        {
            try
            {
                result = operation.Invoke();
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                maxAttempts--;
                if (maxAttempts < 0)
                {
                    throw;
                }
            }
        }

        return result;
    }
}
