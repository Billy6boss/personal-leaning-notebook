using Stage1.Exercises.Ex07_DelegatesAndEvents;

namespace Stage1.Exercises.Tests.Ex07_DelegatesAndEvents;

public class RetryHelperTests
{
    [Fact]
    public void Execute_WhenOperationSucceedsFirstTry_ReturnsResultAndCallsOnce()
    {
        int attempts = 0;
        Func<int> operation = () =>
        {
            attempts++;
            return 42;
        };

        var result = RetryHelper.Execute(operation, maxAttempts: 3);

        Assert.Equal(42, result);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void Execute_RetriesUntilSuccess_WithinMaxAttempts()
    {
        int attempts = 0;
        Func<int> operation = () =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new InvalidOperationException("transient failure");
            }
            return 99;
        };

        var result = RetryHelper.Execute(operation, maxAttempts: 3);

        Assert.Equal(99, result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public void Execute_WhenAllAttemptsFail_ThrowsLastExceptionAndStopsAtMax()
    {
        int attempts = 0;
        Func<int> operation = () =>
        {
            attempts++;
            throw new InvalidOperationException($"failure #{attempts}");
        };

        var ex = Assert.Throws<InvalidOperationException>(() => RetryHelper.Execute(operation, maxAttempts: 2));

        Assert.Equal(2, attempts);
        Assert.Equal("failure #2", ex.Message);
    }

    [Fact]
    public void Execute_WithNonPositiveMaxAttempts_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryHelper.Execute(() => 1, maxAttempts: 0));
    }
}
