namespace Stage1.Exercises.Ex01_ControlFlow;

/// <summary>
/// Stage 1 主題：Variables、data types、operators 與 control flow（流程控制）。
///
/// 任務：
/// 實作 <see cref="Classify"/>，讓它只利用 variables、operators 與
/// control flow（if/else 和/或 switch）依三邊長度分類三角形 —
/// 不能使用 LINQ，也不能使用 pattern matching 語法。
///
/// 規則：
/// - 如果這三邊無法組成合法的三角形，回傳 "Invalid"。
///   （合法的三角形需要所有邊長 &gt; 0，並符合三角不等式（triangle inequality）：
///   任兩邊之和必須嚴格大於第三邊。）
/// - 如果三邊都相等，回傳 "Equilateral"。
/// - 如果恰好兩邊相等，回傳 "Isosceles"。
/// - 如果沒有任何邊相等，回傳 "Scalene"。
/// </summary>
public static class TriangleClassifier
{
    public static string Classify(double sideA, double sideB, double sideC)
    {
        if (sideA <= 0 || sideB <= 0 || sideC <= 0)
        {
            return "Invalid";
        }

        if ((sideA + sideB <= sideC) || (sideB + sideC <= sideA) || (sideA + sideC <= sideB))
        {
            return "Invalid";
        }
        
        if (sideA == sideB && sideB == sideC)
        {
            return  "Equilateral";
        }

        if (sideA == sideB || sideB == sideC || sideC == sideA )
        {
            return "Isosceles";
        }


        return "Scalene";
    }
}
