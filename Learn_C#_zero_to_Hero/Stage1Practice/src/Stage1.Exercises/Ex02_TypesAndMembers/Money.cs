namespace Stage1.Exercises.Ex02_TypesAndMembers;

/// <summary>
/// Stage 1 主題：Methods、classes、records、structs 與 enums。
///
/// 任務：
/// 用 <c>Amount</c>（decimal）與 <c>CurrencyCode</c>（string，例如 "USD"、"TWD"）
/// 來塑模一筆金額。
///
/// 需求：
/// - 必須能以這種方式建構：new Money(10.5m, "USD")
/// - 兩個 Money 實例，只要 Amount 與 CurrencyCode 相同，用 .Equals(...) 比較時
///   必須視為相等；Amount 或 CurrencyCode 不同時則必須視為不相等。
///   （如果你選的 construct 剛好也讓你免費得到 == / !=，很好 —
///   但 .Equals 才是實際會被驗證的 contract。）
/// - Add(Money other) 只有在兩個實例的 CurrencyCode 相同時，才回傳一個
///   Amount 為兩者加總的新 Money。如果幣別不同，要拋出 InvalidOperationException。
/// - Money 的行為應該像一個不可變（immutable）的值：一旦建立，
///   它的 Amount 與 CurrencyCode 就不能再被更改。
///
/// 設計決策：
/// 目前這裡先宣告成一個普通的 "class"。在實作之前，重新思考一下：
/// 依照上面的需求（值相等性、不可變性），"class" 真的是這裡最好的
/// C# construct 嗎？如果你覺得別的選擇更合適，歡迎更換關鍵字
/// （class / record / struct / readonly record struct）—
/// 只要保持 public API（constructor、屬性名稱、Add method）的行為方式不變即可。
/// </summary>
public class Money
{
    // TODO: 實作 Amount、CurrencyCode、constructor，以及 Add(Money other)
    // （把下面會拋出例外的成員換成真正的實作）。
    public decimal Amount => throw new NotImplementedException();

    public string CurrencyCode => throw new NotImplementedException();

    public Money(decimal amount, string currencyCode)
    {
        throw new NotImplementedException();
    }

    public Money Add(Money other)
    {
        throw new NotImplementedException();
    }
}
