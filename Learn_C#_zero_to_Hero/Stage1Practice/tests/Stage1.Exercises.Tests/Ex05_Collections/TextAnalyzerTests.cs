using Stage1.Exercises.Ex05_Collections;

namespace Stage1.Exercises.Tests.Ex05_Collections;

public class TextAnalyzerTests
{
    [Fact]
    public void CountWordFrequency_IsCaseInsensitive_AndIgnoresPunctuation()
    {
        var result = TextAnalyzer.CountWordFrequency("Cat, cat! Dog.");

        Assert.Equal(2, result["cat"]);
        Assert.Equal(1, result["dog"]);
    }

    [Fact]
    public void CountWordFrequency_OnEmptyText_ReturnsEmptyResult()
    {
        var result = TextAnalyzer.CountWordFrequency("");
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("([{}])", true)]
    [InlineData("([)]", false)]
    [InlineData("(()", false)]
    [InlineData("", true)]
    [InlineData("abc(def)[ghi]{jkl}", true)]
    [InlineData("(]", false)]
    public void IsBalanced_DetectsBracketBalance(string expression, bool expected)
    {
        Assert.Equal(expected, TextAnalyzer.IsBalanced(expression));
    }

    [Fact]
    public void FindDuplicates_ReturnsFirstSeenOrder_WithoutRepeats()
    {
        var result = TextAnalyzer.FindDuplicates(new[] { 1, 2, 3, 2, 1, 4 });

        Assert.Equal(new[] { 1, 2 }, result);
    }

    [Fact]
    public void FindDuplicates_WithNoDuplicates_ReturnsEmpty()
    {
        var result = TextAnalyzer.FindDuplicates(new[] { 1, 2, 3 });
        Assert.Empty(result);
    }

    [Fact]
    public void FindDuplicates_WorksWithStrings()
    {
        var result = TextAnalyzer.FindDuplicates(new[] { "a", "b", "a", "c", "c" });
        Assert.Equal(new[] { "a", "c" }, result);
    }
}
