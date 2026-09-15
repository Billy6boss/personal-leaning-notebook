namespace Stage1.Exercises.Ex02_TypesAndMembers;

/// <summary>
/// Stage 1 主題：Methods、classes、records、structs 與 enums。
///
/// 任務：
/// LoyaltyAccount 用來追蹤顧客的 loyalty points（點數）與目前的
/// <see cref="MembershipLevel"/>。跟 Money 不同，這個型別具有識別性
/// （identity），而且狀態會隨時間變動（mutable state），所以這裡
/// 用普通的 "class" 就是恰當的選擇 — 這題不需要更換關鍵字。
///
/// 需求：
/// - Constructor：LoyaltyAccount(string ownerName) 一開始為 0 點，
///   並且是 MembershipLevel.Bronze。
/// - OwnerName（string）與 PointsBalance（int）可以從外部讀取。
/// - CurrentLevel（MembershipLevel）可以從外部讀取，並且永遠依照下方的
///   門檻值與 PointsBalance 保持同步。
/// - AddPoints(int points)：把傳入的點數（必須 &gt; 0，否則要拋出
///   ArgumentOutOfRangeException）加到 PointsBalance，然後根據新的
///   餘額重新評估 CurrentLevel。
///
/// 門檻值（下限為 inclusive）：
///   points &lt; 1_000            -> Bronze
///   1_000  &lt;= points &lt; 5_000   -> Silver
///   5_000  &lt;= points &lt; 20_000  -> Gold
///   points &gt;= 20_000           -> Platinum
/// </summary>
public class LoyaltyAccount
{
    // TODO: 實作 OwnerName、PointsBalance、CurrentLevel、constructor，
    // 以及 AddPoints(int points)（把下面會拋出例外的成員換掉）。
    public string OwnerName {get; init;}

    public int PointsBalance { get; private set; }

    public MembershipLevel CurrentLevel
    {
        get
        {
            return this.PointsBalance switch
            {
                < 1_000 => MembershipLevel.Bronze,
                >= 1_000 and < 5_000 => MembershipLevel.Silver,
                >= 5_000 and < 20_000 => MembershipLevel.Gold,
                >= 20_000 => MembershipLevel.Platinum
            };
        }
    }

    public LoyaltyAccount(string ownerName)
    {
        this.OwnerName = ownerName;
        this.PointsBalance = 0;
    }

    public void AddPoints(int points)
    {
        if (points <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be greater than 0.");
        }

        this.PointsBalance += points;
    }
}
