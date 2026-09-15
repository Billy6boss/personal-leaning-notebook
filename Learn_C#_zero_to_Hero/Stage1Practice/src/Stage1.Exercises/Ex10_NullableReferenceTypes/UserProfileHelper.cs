namespace Stage1.Exercises.Ex10_NullableReferenceTypes;

/// <summary>
/// Stage 1 主題：Nullable reference types。
/// </summary>
public static class UserProfileHelper
{
    /// <summary>
    /// 如果 profile.Nickname 不是 null 且不是空白字元，就回傳它。
    /// 否則，從 profile.Email 推導出一個顯示名稱：取 '@' 字元
    /// 之前的部分（例如 "jane.doe@example.com" -> "jane.doe"）。
    /// 如果 Email 沒有 '@'，就直接原樣回傳 Email 作為 fallback。
    /// 絕對不能拋出 NullReferenceException。
    /// </summary>
    public static string GetDisplayName(UserProfile profile)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 如果 <paramref name="email"/> 是 null 或空白字元，就回傳 null。
    /// 否則回傳 trim 過、轉成小寫的 email。
    /// 請注意參數與回傳型別上的 nullable annotation —
    /// 兩者都應該允許 null，這點跟 GetDisplayName 那個
    /// non-nullable 的 UserProfile 參數不同。
    /// </summary>
    public static string? NormalizeEmail(string? email)
    {
        throw new NotImplementedException();
    }
}
