namespace Stage1.Exercises.Ex10_NullableReferenceTypes;

/// <summary>
/// Stage 1 主題：Nullable reference types。
///
/// 這個專案已經開啟 &lt;Nullable&gt;enable&lt;/Nullable&gt;，
/// 所以如果你的 annotation 寫錯，compiler 會警告你。
///
/// 需求：
/// - Constructor：UserProfile(string email, string? nickname = null)
///   Email 絕對不能是 null（如果傳入 null，要拋出 ArgumentNullException）。
///   Nickname 則是真正的選填欄位，可以是 null。
/// - Email（non-nullable string）可以從外部讀取。
/// - Nickname（nullable string?）可以從外部讀取。
/// 請幫屬性／constructor 參數加上對應的 annotation，讓 compiler
/// 能夠強制這個 contract（正確的呼叫端不會出現 nullable 警告，
/// 誤用時應該是 compile-time 的 nullability 警告，或如上述規格般
/// 在 runtime 拋出 ArgumentNullException）。
/// </summary>
public class UserProfile
{
    // TODO: 實作 constructor 的 null 檢查與驗證邏輯
    // （把下面會拋出例外的 constructor 內容換掉）。屬性型別已經依照
    // 上方規格反映出所需的 nullability。
    public string Email => throw new NotImplementedException();

    public string? Nickname => throw new NotImplementedException();

    public UserProfile(string email, string? nickname = null)
    {
        throw new NotImplementedException();
    }
}
