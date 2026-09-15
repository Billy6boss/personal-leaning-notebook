using Stage1.Exercises.Ex09_PatternMatching;

namespace Stage1.Exercises.Tests.Ex09_PatternMatching;

public class ShapeCalculatorTests
{
    private sealed record UnknownShape : Shape;

    [Fact]
    public void CalculateArea_ForCircle()
    {
        var area = ShapeCalculator.CalculateArea(new Circle(2));
        Assert.Equal(Math.PI * 4, area, 3);
    }

    [Fact]
    public void CalculateArea_ForRectangle()
    {
        var area = ShapeCalculator.CalculateArea(new Rectangle(3, 4));
        Assert.Equal(12, area, 3);
    }

    [Fact]
    public void CalculateArea_ForTriangle()
    {
        var area = ShapeCalculator.CalculateArea(new Triangle(6, 4));
        Assert.Equal(12, area, 3);
    }

    [Fact]
    public void CalculateArea_ForUnknownShape_Throws()
    {
        Assert.Throws<ArgumentException>(() => ShapeCalculator.CalculateArea(new UnknownShape()));
    }
}

public class TemperatureClassifierTests
{
    [Theory]
    [InlineData(-5, "Freezing")]
    [InlineData(0, "Cold")]
    [InlineData(14.999, "Cold")]
    [InlineData(15, "Mild")]
    [InlineData(24.999, "Mild")]
    [InlineData(25, "Warm")]
    [InlineData(34.999, "Warm")]
    [InlineData(35, "Hot")]
    [InlineData(100, "Hot")]
    public void Classify_ReturnsExpectedBand(double celsius, string expected)
    {
        Assert.Equal(expected, TemperatureClassifier.Classify(celsius));
    }
}
