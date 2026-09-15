namespace Stage1.Exercises.Ex09_PatternMatching;

public abstract record Shape;
public sealed record Circle(double Radius) : Shape;
public sealed record Rectangle(double Width, double Height) : Shape;
public sealed record Triangle(double Base, double Height) : Shape;

/// <summary>
/// Stage 1 主題：Pattern matching。
///
/// 任務：
/// 用帶有 type patterns 的 switch EXPRESSION（例如 `Circle c => ...`）
/// 來實作 CalculateArea — 不要用一連串的 `if (shape is Circle)` 陳述式。
/// 遇到無法辨識的 Shape 子型別時，用 discard pattern（`_`）
/// 拋出 ArgumentException。
///
/// 公式：
///   Circle：    PI * Radius^2
///   Rectangle： Width * Height
///   Triangle：  0.5 * Base * Height
/// </summary>
public static class ShapeCalculator
{
    public static double CalculateArea(Shape shape)
    {
        throw new NotImplementedException();
    }
}
