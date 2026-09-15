namespace Stage1.Exercises.Ex03_ValueVsReferenceTypes;

public struct MutablePointStruct
{
    public int X;
    public int Y;
}

public class MutablePointClass
{
    public int X;
    public int Y;
}

/// <summary>
/// Stage 1 主題：Value types 與 reference types。
///
/// 任務：
/// 實作這兩個 overload，讓它們都只是把傳入 point 的 X 加 1
/// （例如 point.X = point.X + 1;）— 邏輯本身故意寫得很單純。
/// 真正有趣的地方在於呼叫端（CALLER）事後會觀察到什麼結果，
/// 因為其中一個參數是 struct（value type），另一個是 class（reference type）。
///
/// 接著實作 <see cref="ExplainBehaviorDifference"/>，回傳一段簡短的說明
/// （用你自己的話，至少一個完整句子），解釋「為什麼」呼叫
/// TryIncrementX(myStruct) 不會改變呼叫端原本的變數，而呼叫
/// TryIncrementX(myClassInstance) 卻會改變呼叫端原本的物件 —
/// 說明時請引用 stack vs heap、copying（複製）、reference semantics
/// （參考語意）等概念。
/// </summary>
public static class MutationDemo
{
    public static void TryIncrementX(MutablePointStruct point)
    {
        throw new NotImplementedException();
    }

    public static void TryIncrementX(MutablePointClass point)
    {
        throw new NotImplementedException();
    }

    public static string ExplainBehaviorDifference()
    {
        throw new NotImplementedException();
    }
}
