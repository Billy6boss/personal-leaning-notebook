using System.Diagnostics;
using Stage1.Exercises.Ex11_AsyncAwait;

namespace Stage1.Exercises.Tests.Ex11_AsyncAwait;

public class QuoteServiceTests
{
    private sealed class FakeQuoteProvider : IQuoteProvider
    {
        private readonly Dictionary<string, TimeSpan> _delays;
        private readonly Dictionary<string, decimal> _prices;
        private readonly string? _symbolThatThrows;

        public FakeQuoteProvider(
            Dictionary<string, TimeSpan> delays,
            Dictionary<string, decimal> prices,
            string? symbolThatThrows = null)
        {
            _delays = delays;
            _prices = prices;
            _symbolThatThrows = symbolThatThrows;
        }

        public async Task<Quote> FetchQuoteAsync(string symbol, CancellationToken cancellationToken)
        {
            await Task.Delay(_delays[symbol], cancellationToken);

            if (symbol == _symbolThatThrows)
            {
                throw new InvalidOperationException($"Simulated failure for {symbol}");
            }

            return new Quote(symbol, _prices[symbol]);
        }
    }

    [Fact]
    public async Task GetQuotesAsync_FetchesConcurrently_NotSequentially()
    {
        var symbols = new[] { "A", "B", "C", "D", "E" };
        var delays = symbols.ToDictionary(s => s, _ => TimeSpan.FromMilliseconds(200));
        var prices = symbols.ToDictionary(s => s, _ => 1m);
        var provider = new FakeQuoteProvider(delays, prices);

        var stopwatch = Stopwatch.StartNew();
        var results = await QuoteService.GetQuotesAsync(provider, symbols);
        stopwatch.Stop();

        Assert.Equal(5, results.Count);
        Assert.True(
            stopwatch.ElapsedMilliseconds < 600,
            $"Expected concurrent fetches to finish well under 1000ms, took {stopwatch.ElapsedMilliseconds}ms. " +
            "This usually means the symbols were awaited one-by-one instead of concurrently.");
    }

    [Fact]
    public async Task GetQuotesAsync_PreservesInputOrder_RegardlessOfCompletionOrder()
    {
        var delays = new Dictionary<string, TimeSpan>
        {
            ["Slow"] = TimeSpan.FromMilliseconds(200),
            ["Fast"] = TimeSpan.FromMilliseconds(10),
        };
        var prices = new Dictionary<string, decimal>
        {
            ["Slow"] = 100m,
            ["Fast"] = 200m,
        };
        var provider = new FakeQuoteProvider(delays, prices);

        var results = await QuoteService.GetQuotesAsync(provider, new[] { "Slow", "Fast" });

        Assert.Equal("Slow", results[0].Symbol);
        Assert.Equal("Fast", results[1].Symbol);
    }

    [Fact]
    public async Task GetQuotesAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        var symbols = new[] { "A", "B" };
        var delays = symbols.ToDictionary(s => s, _ => TimeSpan.FromSeconds(5));
        var prices = symbols.ToDictionary(s => s, _ => 1m);
        var provider = new FakeQuoteProvider(delays, prices);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => QuoteService.GetQuotesAsync(provider, symbols, cts.Token));
    }

    [Fact]
    public async Task GetQuotesAsync_WhenOneFetchFails_PropagatesException()
    {
        var symbols = new[] { "A", "B", "C" };
        var delays = symbols.ToDictionary(s => s, _ => TimeSpan.FromMilliseconds(10));
        var prices = symbols.ToDictionary(s => s, _ => 1m);
        var provider = new FakeQuoteProvider(delays, prices, symbolThatThrows: "B");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => QuoteService.GetQuotesAsync(provider, symbols));
    }
}
