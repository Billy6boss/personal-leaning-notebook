# 《7 個我從資深工程師身上偷來的程式設計模式》繁體中文總結與評論

> 原文：7 Coding Patterns I Stole From Senior Engineers

---

## 📋 文章總結

這篇文章的核心觀點是：**資深工程師並非懂更多技巧，而是製造更少問題**。

作者觀察到，資深工程師的程式碼看起來「無聊」，沒有炫技的抽象層，但每當需求改變，這些程式碼卻能存活下來。以下是文章整理的七個核心模式：

---

### 1. 提早回傳，取代巢狀條件迷宮（Guard Clauses）

與其讓所有錯誤條件把真正的業務邏輯推到六層縮排深處，不如先把「不符合條件的路徑」在最前面就清除掉。Guard Clauses 讓失敗路徑一眼可見，讓 Happy Path 保持乾淨。

**核心思想**：閱讀程式碼的代價比撰寫更高，要為「壓力下閱讀程式的人」最佳化。

#### ❌ 錯誤示範（巢狀條件）

```csharp
public async Task<UserProfile> UpdateUserProfileAsync(string userId, ProfileInput input)
{
    var user = await GetUserAsync(userId);

    if (user != null)
    {
        if (!string.IsNullOrEmpty(input.Email))
        {
            if (user.CanEditProfile)
            {
                return await SaveProfileAsync(user.Id, input);
            }
        }
    }
    throw new Exception("Unable to update profile");
}
```

#### ✅ 正確示範（Guard Clauses）

```csharp
public async Task<UserProfile> UpdateUserProfileAsync(string userId, ProfileInput input)
{
    var user = await GetUserAsync(userId);

    if (user == null)
        throw new NotFoundException("User not found");

    if (string.IsNullOrEmpty(input.Email))
        throw new ValidationException("Email is required");

    if (!user.CanEditProfile)
        throw new ForbiddenException("User cannot edit profile");

    return await SaveProfileAsync(user.Id, input);
}
```

---

### 2. 命名業務意義，而非技術實作（Meaningful Names）

用 `data`、`result`、`item` 命名是把複雜度轉嫁到下一個人的記憶裡。資深工程師以業務意義命名（如 `subscription`、`usersEligibleForReactivation`），讓命名對「誤用」產生摩擦力，減少錯誤假設的發生空間。

**核心思想**：好的命名讓下一個開發者不需要從實作中逆向推導意圖。

#### ❌ 錯誤示範（技術名稱）

```csharp
var result = await GetDataAsync(id);
if (result.Status == "active")
    await ProcessAsync(result);
```

#### ✅ 正確示範（業務名稱）

```csharp
var subscription = await GetSubscriptionAsync(subscriptionId);
if (subscription.IsBillable)
    await ChargeSubscriptionAsync(subscription);

// 名字可以很長，因為業務規則是具體的
var usersEligibleForReactivation = await FindUsersEligibleForReactivationAsync();
```

---

### 3. 在外部混亂周圍建立邊界（Anti-Corruption Layer）

第三方 API 的欄位名稱、格式、結構不應直接散落在整個程式庫中。資深工程師會建立一個轉換函式（Adapter/Mapper），把「外部的語言」轉換成「自己系統的語言」，讓修改範圍縮小到單一邊界點。

**核心思想**：永遠不要讓你無法掌控的系統，決定你可以掌控的系統的形狀。

#### ❌ 錯誤示範（外部結構四散）

```csharp
// 這段程式散落在 Controller、Service、Job 各處
var userName = response.Data.user_name;
var isActive = response.Data.status == "ACTIVE";
var plan = response.Data.subscription?.plan_name;
```

#### ✅ 正確示範（集中在 Mapper 邊界）

```csharp
// 外部 API 回應的原始結構（不受我們控制）
public class BillingCustomerResponse
{
    public string id { get; set; }
    public string user_name { get; set; }
    public string status { get; set; }
    public BillingSubscription? subscription { get; set; }
}

// 系統內部使用的 Domain Model（我們掌控）
public class Customer
{
    public string Id { get; init; }
    public string Name { get; init; }
    public bool IsBillable { get; init; }
    public string PlanName { get; init; }
}

// 只有這一個地方知道外部格式的細節
public Customer MapBillingCustomer(BillingCustomerResponse response)
{
    return new Customer
    {
        Id = response.id,
        Name = response.user_name,
        IsBillable = response.status == "ACTIVE",
        PlanName = response.subscription?.plan_name ?? "Free"
    };
}
```

---

### 4. 讓非法狀態變得無聊且困難（Make Invalid States Unrepresentable）

把所有欄位都設為可為 null 只是讓編譯器不再抱怨，不是真正的型別安全。資深工程師會明確建模不同的狀態（`DraftUser` vs `SavedUser`），讓「不存在的狀態」在型別系統中根本無法被表達。

**核心思想**：不只是處理非法狀態，而是設計讓非法狀態沒有藏身之處的程式碼。

#### ❌ 錯誤示範（到處都要防禦性檢查）

```csharp
// 所有欄位都可為 null，每個呼叫端都需要自行防禦
public class User
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
}
```

#### ✅ 正確示範（用型別區分狀態）

```csharp
public enum UserRole { Admin, Member }
public enum UserStatus { Active, Disabled }

// 尚未儲存的草稿使用者：沒有 Id、沒有 Status
public class DraftUser
{
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
}

// 已儲存的使用者：Id 與 Status 一定存在
public class SavedUser
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public required UserStatus Status { get; init; }
}
```

#### ✅ 進階示範（用 sealed class 表達支付狀態機）

```csharp
// 用繼承階層讓非法狀態組合無法被建構
public abstract record Payment(string Id);

public sealed record PendingPayment(string Id) : Payment(Id);
public sealed record AuthorizedPayment(string Id, string AuthorizationId) : Payment(Id);
public sealed record CapturedPayment(string Id, string ReceiptId) : Payment(Id);
public sealed record FailedPayment(string Id, string Reason) : Payment(Id);

// 寄送收據只接受已 Captured 的付款，編譯期就強制保證
public void SendReceipt(CapturedPayment payment)
{
    EmailReceipt(payment.ReceiptId);
}

// 呼叫端必須先用 pattern matching 確認狀態才能呼叫
if (payment is CapturedPayment captured)
    SendReceipt(captured);
```

---

### 5. 將決策與行動分離（Separate Decisions from Actions）

把「是否可以退款」的判斷邏輯抽成純函式（Pure Function），讓業務規則可以不依賴任何 mock 就能單獨測試。執行側的函式只負責「執行已決定的事」，不再夾雜條件判斷。

**核心思想**：決策應該可以在不觸發其控制的副作用的情況下被測試。

#### ❌ 錯誤示範（決策與行動混在一起）

```csharp
// 測試退款規則時，必須 mock DB、金流 Provider、Email Service
public async Task RefundInvoiceAsync(string invoiceId)
{
    var invoice = await GetInvoiceAsync(invoiceId);

    if (invoice.Status != "paid")
        throw new Exception("Invoice cannot be refunded");

    if (invoice.RefundedAt.HasValue)
        throw new Exception("Invoice already refunded");

    if (invoice.Amount <= 0)
        throw new Exception("Invalid refund amount");

    await _paymentProvider.RefundAsync(invoice.PaymentId);
    await MarkInvoiceRefundedAsync(invoice.Id);
    await SendRefundEmailAsync(invoice.CustomerId);
}
```

#### ✅ 正確示範（決策抽成純函式）

```csharp
public record RefundEligibility(bool Allowed, string? Reason = null);

// 純函式：不依賴任何外部資源，單元測試不需要任何 mock
public RefundEligibility GetRefundEligibility(Invoice invoice)
{
    if (invoice.Status != "paid")
        return new RefundEligibility(false, "Invoice is not paid");

    if (invoice.RefundedAt.HasValue)
        return new RefundEligibility(false, "Invoice is already refunded");

    if (invoice.Amount <= 0)
        return new RefundEligibility(false, "Invalid refund amount");

    return new RefundEligibility(true);
}

// 行動函式：只負責執行，不做業務判斷
public async Task RefundInvoiceAsync(string invoiceId)
{
    var invoice = await GetInvoiceAsync(invoiceId);
    var eligibility = GetRefundEligibility(invoice);

    if (!eligibility.Allowed)
        throw new ValidationException(eligibility.Reason!);

    await _paymentProvider.RefundAsync(invoice.PaymentId);
    await MarkInvoiceRefundedAsync(invoice.Id);
    await SendRefundEmailAsync(invoice.CustomerId);
}
```

```csharp
// 測試業務規則不需要任何 mock，極度簡潔
[Fact]
public void GetRefundEligibility_WhenNotPaid_ReturnNotAllowed()
{
    var invoice = new Invoice { Status = "pending", Amount = 100 };
    var result = GetRefundEligibility(invoice);
    Assert.False(result.Allowed);
    Assert.Equal("Invoice is not paid", result.Reason);
}
```

---

### 6. 讓錯誤對下一個人有用（Useful Errors）

「Something went wrong」對任何人都沒有幫助。錯誤訊息是一種溝通：要給機器用的（錯誤碼）和給人看的（說明）都要有，並且在 log 中記錄足夠的上下文，讓事故調查從猜謎變成可追蹤的路徑。

**核心思想**：錯誤應該幫助下一個人了解什麼失敗了、在哪裡失敗了，以及有哪些證據。

#### ❌ 錯誤示範（對機器和人都無用）

```json
{ "message": "Something went wrong" }
```

```csharp
// 前端被迫用字串比對來判斷錯誤類型，是一個人質情境
if (error.Message.Contains("already exists"))
    ShowEmailTakenError();
```

#### ✅ 正確示範（結構化錯誤回應）

```csharp
public class ApiErrorResponse
{
    public string Code { get; init; }       // 給機器用：系統可靠地判斷錯誤類型
    public string Message { get; init; }    // 給人看：說明發生了什麼
    public Dictionary<string, string>? Details { get; init; }  // 額外上下文（如哪個欄位有問題）
    public string RequestId { get; init; }  // 串接 log 用
}

// 回應範例
// {
//   "code": "USER_EMAIL_ALREADY_EXISTS",
//   "message": "A user with this email already exists.",
//   "details": { "field": "email" },
//   "requestId": "req_8f91a2"
// }
```

#### ✅ 正確示範（結構化 Log）

```csharp
// 所有關鍵上下文在同一行，方便 log 搜尋與關聯
_logger.LogWarning(
    "Refund rejected | invoiceId={InvoiceId} | customerId={CustomerId} | reason={Reason} | requestId={RequestId}",
    invoiceId, customerId, eligibility.Reason, requestId);
```

---

### 7. 為 Diff 最佳化，而非為 Demo（Optimize for the Diff）

功能在本機跑通不代表工作結束。資深工程師會把大 PR 拆成小的、有焦點的 PR（重構改一次、行為改一次、UI 改一次），讓程式碼審查可以說出一個清楚的故事，讓問題能被追蹤，讓回滾不會傷及無辜。

**核心思想**：程式碼在你的機器上跑通時還沒完成，要等到這個改動是可理解的、可審查的、可測試的、且可安全回滾的，才算完成。

#### ❌ 錯誤示範（一個 PR 什麼都改）

```
feat: update billing flow
- refactor invoice service
- rename payment fields
- update refund logic
- change dashboard UI
- add new webhook handler
- modify retry behavior
- fix customer status bug
- update tests
```

#### ✅ 正確示範（每個 PR 只說一件事）

```
PR 1: 重新命名 Payment 欄位（純重構，無行為變更）
PR 2: 新增 GetRefundEligibility 純函式與單元測試
PR 3: 將退款流程接入 GetRefundEligibility
PR 4: 更新 Dashboard UI 顯示退款原因
PR 5: 新增 Webhook 重試機制
```

> 以打字時間衡量，這看起來比較慢。以 Review + Debug + Rollback 衡量，這是更快的路徑。

---

### 七個模式背後的共同思想

> **減少驚喜（Reduce Surprise）**

複雜度必然存在，但如果沒有放在清楚的命名、明確的邊界、誠實的型別、聚焦的函式、有用的錯誤和小型的 diff 裡，它就會擴散進每個人的腦袋，讓程式庫變得令人疲憊。

---

## 🎯 資深工程師評論

### 整體評價

這篇文章的強項在於它把「經驗值」翻譯成了可執行的原則。這七個模式不是什麼新發明——它們分別對應軟體工程中已被廣泛討論的概念（Guard Clauses、Ubiquitous Language、Anti-Corruption Layer、Make Illegal States Unrepresentable、Command-Query Separation、Structured Logging、小型 PR 文化）。但難得的是，文章把這些概念用實際工作場景的「痛點」串聯起來，而不是用學術定義堆砌。

---

### 值得肯定的地方

**模式 3（建立邊界）和模式 4（讓非法狀態困難）是文章最有重量的兩點。**

邊界（Anti-Corruption Layer）的缺失是很多中大型系統技術債的根源。一個欄位名稱直接散落在十幾個模組裡，才會造成文章描述的「找到所有依賴點才是痛苦所在」。這個教訓很多人是在生產事故後才真正體會的。

「讓非法狀態無法被表達」則是比「防禦性程式設計」更高層次的思維。與其在每個呼叫端加上防護，不如讓錯誤的組合在型別層面就不存在。這在強型別語言中（TypeScript、Rust、Haskell）可以執行得很徹底，在動態語言中則需要靠 Schema 驗證與工廠模式達到類似效果。

**模式 7（為 Diff 最佳化）點出了很多技術主管才會意識到的問題。** 一個混雜了重構、行為變更和 bug fix 的 PR，在事故時會讓整個團隊陷入「到底是哪個改動造成的」的困境。拆分 PR 看似慢，但加入 review 時間、debug 時間和 rollback 風險後，是更快的路徑。

---

### 可以更深入的地方

**模式 5（決策與行動分離）**值得被更嚴謹地框架化。文章的例子停留在「把 if 條件抽成純函式」的層次，但這背後其實是更廣泛的 **CQS（Command-Query Separation）** 和 **Functional Core, Imperative Shell** 的設計哲學。資深工程師真正在做的，是盡量把業務邏輯推向沒有副作用的「純粹計算」區域，讓不純的 I/O 操作退到程式的邊緣。這個思維模型在處理複雜業務流程時，會比「抽一個 helper function」帶來更系統性的架構改善。

**模式 2（有意義的命名）**的討論可以再往下一層。文章提到「命名業務意義」，但沒有提到更難的問題：**什麼是正確的業務語言？** 這直接關聯到 DDD（領域驅動設計）中的 Ubiquitous Language——命名不只是開發者的選擇，而是需要和業務方對齊的協作產物。當工程師和 PM 對同一個概念有不同的詞彙，程式碼裡就會出現多套名字指向同一件事，這才是真正的命名問題根源。

**關於例外情況的討論略顯保守。** 文章在大多數模式後面都加了「但是有例外」的免責聲明，這是正確的，但有時候反而模糊了判斷準則。更實用的說法是：**這些模式的適用邊界通常是「有多個呼叫端」和「業務規則會變動」**。當一個函式只被呼叫一次、邏輯固定，就不需要為它建立邊界或抽取決策函式。

---

### 給台灣工程師的補充觀察

這篇文章的七個模式，本質上都在解決同一個問題：**讓程式碼的意圖對陌生人可見**。在台灣開發環境中，有幾個常見的情境特別需要這些原則：

1. **高流動率團隊**：新人上手時間長、老人離職帶走大量隱性知識，正是這些模式能解決的場景。
2. **快速迭代文化**：需求頻繁變更、時間壓力大，恰恰是最需要「Guard Clauses 讓意圖清晰」和「小 PR 讓回滾安全」的環境。
3. **第三方整合密集的電商/SaaS 系統**：金流、物流、簡訊、推播——每一個都是不穩定的外部邊界，模式 3 的邊界思維在這種系統中的投資報酬率極高。

---

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐☆☆ |
| 新穎性 | ⭐⭐⭐☆☆ |
| 適合對象 | 初階至中階工程師 |

**這是一篇很好的「補齊直覺」的文章，適合作為 Code Review 文化建立的敲門磚。** 如果想更系統性地深入，可以延伸閱讀：
- *A Philosophy of Software Design* — John Ousterhout（對複雜度的更深層分析）
- *Domain-Driven Design* — Eric Evans（模式 2、3 的理論基礎）
- *Functional Design* — Robert C. Martin（模式 4、5 的函數式視角）
- *Accelerate* — Nicole Forsgren（模式 7 的數據支撐）
