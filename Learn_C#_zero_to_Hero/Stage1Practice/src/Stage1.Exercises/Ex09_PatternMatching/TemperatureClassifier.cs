namespace Stage1.Exercises.Ex09_PatternMatching;

/// <summary>
/// Stage 1 主題：Pattern matching。
///
/// 任務：
/// 用帶有 relational patterns（&lt;、&gt;= 等）與邏輯 pattern
/// combinators（`and`、`or`）的 switch EXPRESSION 來實作 Classify，
/// 而不是一連串的 if/else-if 比較。
///
/// 區間（攝氏 Celsius）：
///   celsius &lt; 0            -> "Freezing"
///   0  &lt;= celsius &lt; 15     -> "Cold"
///   15 &lt;= celsius &lt; 25     -> "Mild"
///   25 &lt;= celsius &lt; 35     -> "Warm"
///   celsius &gt;= 35          -> "Hot"
/// </summary>
public static class TemperatureClassifier
{
    public static string Classify(double celsius)
    {
        throw new NotImplementedException();
    }
}
