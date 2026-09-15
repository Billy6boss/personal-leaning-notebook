using Stage1.Exercises.Ex03_ValueVsReferenceTypes;

namespace Stage1.Exercises.Tests.Ex03_ValueVsReferenceTypes;

public class MutationDemoTests
{
    [Fact]
    public void TryIncrementX_OnStruct_DoesNotAffectCallersCopy()
    {
        var point = new MutablePointStruct { X = 1, Y = 1 };

        MutationDemo.TryIncrementX(point);

        Assert.Equal(1, point.X);
    }

    [Fact]
    public void TryIncrementX_OnClass_AffectsSharedInstance()
    {
        var point = new MutablePointClass { X = 1, Y = 1 };

        MutationDemo.TryIncrementX(point);

        Assert.Equal(2, point.X);
    }

    [Fact]
    public void ExplainBehaviorDifference_ReturnsANonTrivialExplanation()
    {
        var explanation = MutationDemo.ExplainBehaviorDifference();

        Assert.False(string.IsNullOrWhiteSpace(explanation));
        Assert.True(explanation.Length >= 20, "Explanation should be a real sentence, not a placeholder.");

        // 注意：這裡只會檢查「有寫東西」而已。你的說明內容是否正確
        // （有沒有提到 stack/heap、copying、reference semantics 等概念）
        // 會由你的老師人工審閱，而不是這個自動化測試。
    }
}
