namespace Stage1.Exercises.Ex11_AsyncAwait;

public record Quote(string Symbol, decimal Price);

public interface IQuoteProvider
{
    Task<Quote> FetchQuoteAsync(string symbol, CancellationToken cancellationToken);
}

/// <summary>
/// Stage 1 主題：用 async 與 await 進行非同步（asynchronous）程式設計。
///
/// 任務：
/// 實作 GetQuotesAsync，讓它透過呼叫 provider.FetchQuoteAsync，
/// 針對 <paramref name="symbols"/> 中每一個 symbol 都取得一筆報價。
///
/// 需求：
/// - 所有 symbol 必須「同時（CONCURRENTLY）」抓取，不能一個一個依序抓
///   （想想看哪個 Task combinator 可以讓你同時 await 多個進行中的
///   task）。
/// - 結果的順序必須跟輸入的 symbols 順序「相同」，
///   不管哪一個先抓取完成。
/// - 必須從頭到尾都是真正的非同步：你的實作裡不能有 .Result、
///   .Wait()，或任何 blocking 呼叫。
/// - cancellationToken 必須傳遞給每一次 FetchQuoteAsync 呼叫，
///   如果這個 token 已經被取消（或在抓取過程中被取消），
///   回傳的 Task 應該要以 OperationCanceledException 失敗／取消，
///   而不是悄悄回傳部分資料。
/// - 如果任何一個 fetch 拋出例外，這個例外應該要往外傳播
///   （propagate）出 GetQuotesAsync（不要吞掉它）。
/// </summary>
public static class QuoteService
{
    public static Task<IReadOnlyList<Quote>> GetQuotesAsync(
        IQuoteProvider provider,
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
