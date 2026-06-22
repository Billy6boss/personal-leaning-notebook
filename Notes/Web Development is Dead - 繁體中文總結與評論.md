# 《Web Development is Dead… And That's Your Opportunity》繁體中文總結與評論

> 原文：Web Development is Dead… And That's Your Opportunity

---

## 📋 文章總結

這篇文章的核心主張是：Web 開發市場飽和的不是「開發者」，而是「會跟著教學做的人」。當 95% 的人都走相同的路徑（Bootcamp → React → 仿作 Portfolio → 海投），市場自然會對這類履歷免疫。然而，這對真正在意「把東西做好」的人反而是機會——門檻提高本身就是一種篩選器。

文章同時指出，公司徵才看的從來不是你用哪個框架，而是你能否在需求模糊的情況下做出合理決策、在系統腐化之前看到問題。大多數人跳過了這個部分，因為它沒有 YouTube 縮圖、沒有完課勳章。

---

### 1. Tutorial Hell — 為什麼練習感覺很有用，但實際上沒有（Tutorial Hell）

大多數人學習 Web 開發的方式是「完成課程」而非「解決問題」。教學影片讓人獲得完課的多巴胺，但真實開發的日常是：需求模糊、相依套件衝突、架構六個月後變成一團亂、debug 毫無邏輯的問題。教學影片從來不練這些。只有當你需要自己做決定的時候，你才真的在學開發。

**核心思想**：重複做教學範例是練習「模仿」，不是練習「開發」。

#### ❌ Tutorial Hell 的程式碼樣子

```csharp
// 直接照抄 StackOverflow，沒有理解為什麼這樣寫
public async Task<List<Product>> GetProductsAsync()
{
    using var context = new AppDbContext();
    return await context.Products.ToListAsync(); // 每次都 new context？N+1 沒概念？
}
```

#### ✅ 真正理解後的寫法

```csharp
// 理解 DI、連線池、非同步邊界，知道 ToListAsync 在什麼情境下會拖垮效能
public class ProductService(AppDbContext context)
{
    public async Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken ct = default)
    {
        // 明確只撈需要的欄位，避免 over-fetching
        return await context.Products
            .Where(p => p.IsActive)
            .AsNoTracking()   // 只讀情境不需要 change tracking
            .ToListAsync(ct);
    }
}
```

---

### 2. 公司要的不是「框架操作員」（Fundamentals Over Frameworks）

徵才頁面寫「React Developer」，但公司真正想要的是：能在沒有逐步教學的情況下把模糊需求變成可用的東西、能理解 trade-off、能在框架退流行之後還活著的人。框架每幾年就換一輪，但理解閉包、非同步、記憶體模型的人在每個時代都值錢。

**核心思想**：框架是可替換的工具，底層理解才是跨框架的競爭力。

#### ❌ 只會框架，不懂底層

```csharp
// 知道 async/await 怎麼寫，但不知道為什麼這樣會 deadlock
public string GetData()
{
    // .Result 在 ASP.NET 同步環境中會死結，但「教學上這樣可以跑」
    return _httpClient.GetStringAsync("https://api.example.com/data").Result;
}
```

#### ✅ 理解底層後的判斷

```csharp
// 知道 ConfigureAwait、同步上下文、以及什麼時候真的需要同步呼叫
public async Task<string> GetDataAsync(CancellationToken ct = default)
{
    // 正確傳遞 CancellationToken，讓呼叫鏈可以被取消
    return await _httpClient.GetStringAsync("https://api.example.com/data", ct);
}

// 若非得要同步介面（例如實作舊介面），用 GetAwaiter().GetResult() 並知道風險
```

---

### 3. 大家的 Portfolio 都長一樣（Stand Out by Being Real）

當面試官看了 500 份履歷，其中 470 份都是 Netflix Clone 和 Todo App，他們對 Portfolio 就免疫了。問題不是「做 side project」這個建議本身，而是大家用最省力的方式解讀它——做沒有真實約束的仿作。

一個「幫朋友的小餐廳做了訂位系統，然後被真實使用者投訴過」的開發者，和一個「跟著 YouTube 做了電商模板」的開發者，市場給出的估值會完全不同。

**核心思想**：真實世界的約束（預算、真實使用者、罕見邊界條件）是教學環境無法複製的訓練。

#### 從「仿作思維」到「真實約束思維」的差距

```csharp
// 仿作思維：能跑就好，沒有考慮過真實場景
public void BookTable(int tableId, DateTime time)
{
    _db.Reservations.Add(new Reservation { TableId = tableId, Time = time });
    _db.SaveChanges();
}

// 真實約束思維：被真實使用者罵過之後，你會知道要處理這些
public async Task<Result<ReservationId>> BookTableAsync(
    int tableId,
    DateTimeOffset requestedTime,
    int partySize,
    CancellationToken ct = default)
{
    // 並發衝突：兩個人同時訂同一桌
    var isAlreadyBooked = await _db.Reservations
        .AnyAsync(r => r.TableId == tableId
                    && r.Time == requestedTime
                    && r.Status != ReservationStatus.Cancelled, ct);

    if (isAlreadyBooked)
        return Result.Fail<ReservationId>("該時段已被預訂");

    // 營業時間驗證：晚上 11 點後不接受預訂
    if (requestedTime.Hour >= 23 || requestedTime.Hour < 10)
        return Result.Fail<ReservationId>("超出營業時間");

    var reservation = new Reservation
    {
        TableId = tableId,
        Time = requestedTime,
        PartySize = partySize,
        Status = ReservationStatus.Pending
    };
    _db.Reservations.Add(reservation);
    await _db.SaveChangesAsync(ct);

    return Result.Ok(new ReservationId(reservation.Id));
}
```

---

### 4. 廣而不深是最常見的錯誤（Depth Over Breadth）

「Full Stack Developer」在 Junior 層級幾乎已變成無意義的標籤——每樣都會一點，沒有一樣真的夠深。文章指出幾個被嚴重低估的專精方向：Accessibility、效能優化、資安、複雜狀態管理、測試基礎建設。這些東西平時沒人提，但公司某天突然急需時，就是你的護城河。

**核心思想**：在某個「無聊但重要」的領域做到真正深入，比每樣都會一點有價值得多。

#### 舉例：大多數人跳過的效能思維

```csharp
// 「能跑就好」的寫法：每次請求都打 DB，沒有考慮過規模
public async Task<CategoryDto[]> GetCategoriesAsync()
{
    return await _db.Categories
        .Select(c => new CategoryDto(c.Id, c.Name))
        .ToArrayAsync();
}

// 對效能有深度理解的寫法：分類清單幾乎不變，加快取並控制失效
public async Task<CategoryDto[]> GetCategoriesAsync(CancellationToken ct = default)
{
    const string cacheKey = "categories:all";

    if (_cache.TryGetValue(cacheKey, out CategoryDto[]? cached))
        return cached!;

    var categories = await _db.Categories
        .AsNoTracking()
        .Select(c => new CategoryDto(c.Id, c.Name))
        .ToArrayAsync(ct);

    _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(10));
    return categories;
}
```

---

### 5. 市場只會更難，這是好事（Harder Market = Better Filter）

AI 工具會取代部分低層次的工作，公司也因為選擇更多而變得更挑剔。「速成」時代大概結束了。但文章的結論是：這對真正在意把東西做好的人是好消息——因為留下來的人，是被迫真正學會事情如何運作的人。淘汰機制讓剩下的人更值錢。

**核心思想**：更嚴格的市場是比任何課程都更有效的篩選器，活下來的人通常更強，不是因為更聰明，而是因為被迫真正學習。

---

## 🎯 資深軟體工程師兼技術主管（Tech Lead）評論

### 整體評價

這篇文章的定位是「清醒劑」而非技術指南，它試圖打破「跟著教學走就能找到工作」的錯誤期待。以這個目標來說，文章達到了效果——它清楚說出了很多招募主管心裡有但不會公開說的話。

身為曾經面試過大量求職者的人，「470 份 Portfolio 長得一樣」這個描述幾乎是逐字逐字地符合現實。差別在於，這篇文章把問題的根因說對了：問題不是「做 side project」的建議本身，而是人們在沒有真實約束的情況下練習，練出來的是「模仿能力」不是「工程判斷力」。

### 值得肯定的地方

**「公司不是在找框架操作員」這個論點值得被更廣泛傳播。** 招募時最難篩選的，不是「會不會 React」，而是「給你一個模糊的需求，你能不能在沒有人牽著手的情況下做出合理的決定」。這種判斷力只能從真實的失敗中練出來，教學影片永遠無法傳授。

**「無聊的專精方向」的觀察也很精準。** Accessibility、效能優化、測試基礎建設——這些領域幾乎沒有人靠自學刷到，但在真實的商業系統中極度稀缺。一個真的懂 .NET 記憶體模型、會看 dump 的工程師，在台灣市場的議價能力遠比一個會用所有 JS 框架的人高。

### 可以更深入的地方

**文章沒有給出具體的「怎麼做」。** 說「做有真實約束的專案」是對的，但對一個剛起步的人來說，「怎麼找到有真實約束的機會」是更難的問題。開源貢獻、接小型 Freelance、幫非營利組織做系統——這些路徑都值得被點名。

**關於 AI 工具取代工作的部分，文章明顯保守帶過。** 在 2025 年的現實是：AI 已經能獨立完成大量過去需要 Junior 工程師完成的「翻譯型工作」（把需求翻譯成 CRUD API、翻譯成簡單的前端元件）。這個衝擊對「會跟著教學做」的族群是存在性威脅，而對能做系統決策的人則幾乎沒有影響。文章可以更直白地說清楚這個分水嶺。

**「Full Stack 沒意義」的論點也值得更細緻。** 在台灣中小型公司的現實中，Full Stack 仍然是必要的——但差別在於你是「每樣都懂一點的萬用型工讀生」，還是「有一個核心深度、其他方向能溝通的工程師」。兩者在市場上的價值是天壤之別。

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐☆ |
| 深度 | ⭐⭐⭐☆☆ |
| 新穎性 | ⭐⭐☆☆☆ |
| 適合對象 | 初階至中階工程師（尤其是正在 Tutorial Hell 中的人） |

**延伸閱讀**：
- *The Pragmatic Programmer* — Andrew Hunt & David Thomas（「真實開發者思維」的經典，比任何職涯建議文章都深）
- *A Philosophy of Software Design* — John Ousterhout（理解複雜度是什麼，以及為什麼「讓東西能跑」不等於「把東西做好」）
- *Staff Engineer* — Will Larson（想知道「深度」這條路通往哪裡，這本書有答案）
- *Software Engineering at Google* — Titus Winters et al.（測試、維護性、規模——那些「無聊但重要」的話題在這裡有完整論述）
