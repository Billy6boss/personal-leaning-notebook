using Stage1.Exercises.Ex01_ControlFlow;

namespace Stage1.Exercises.Tests.Ex01_ControlFlow;

public class TriangleClassifierTests
{
    [Fact]
    public void Equilateral_WhenAllSidesEqual()
    {
        Assert.Equal("Equilateral", TriangleClassifier.Classify(3, 3, 3));
    }

    [Fact]
    public void Isosceles_WhenExactlyTwoSidesEqual()
    {
        Assert.Equal("Isosceles", TriangleClassifier.Classify(3, 3, 4));
    }

    [Fact]
    public void Scalene_WhenNoSidesEqual()
    {
        Assert.Equal("Scalene", TriangleClassifier.Classify(2, 3, 4));
    }

    [Theory]
    [InlineData(1, 1, 3)]   // 違反 triangle inequality（三角不等式）
    [InlineData(0, 3, 4)]   // 邊長為 0
    [InlineData(-1, 3, 4)]  // 邊長為負數
    [InlineData(2, 2, 4)]   // 退化情況：兩邊之和等於第三邊
    public void Invalid_ForImpossibleTriangles(double a, double b, double c)
    {
        Assert.Equal("Invalid", TriangleClassifier.Classify(a, b, c));
    }
}
