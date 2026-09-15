# .NET 11：Program.cs 不再是整個 pipeline 的全貌

## 一、核心結論

過去 ASP.NET Core 的 pipeline 很容易理解：`app.Use()` 之後就是依序跑，而且你可以從 `Program.cs` 看出每一段請求會經過什麼中間件。

但在 .NET 11（Preview 6）之後，這個前提被打破了：

- 應用程式會自動掛上一層 CSRF 防護
- Kestrel 對 malformed request 的處理方式改變
- ASP.NET Core 會自動寫入 OpenTelemetry 屬性
- 驗證流程可以在 endpoint 執行前做 async DB 檢查

也就是說，`Program.cs` 只能告訴你「你註冊了什麼」，不再能完全代表「請求實際會經過什麼」。

---

## 二、.NET 11 預設的 CSRF 防護

### 1. 你可能沒有明確註冊 antiforgery，仍然被保護

像這樣的程式：

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/widgets", ([FromForm] Widget w) =>
    Results.Created($"/widgets/{w.Id}", w));

app.Run();
```

在 .NET 11 預設情況下，這個 endpoint 會被 CSRF 保護。

條件如下：

- 沒有 `AddAntiforgery()`
- 沒有 `UseAntiforgery()`
- 沒有 token 流程
- 也沒有 data protection 依賴

它不是靠同步 token 機制，而是依賴瀏覽器自動帶上的 `Origin` 與 `Sec-Fetch-Site` headers。

### 2. 判定邏輯很簡單：第一個符合條件的規則勝出

規則順序：

- `GET` / `HEAD` / `OPTIONS` / `TRACE`：放行
- `Sec-Fetch-Site: same-origin` 或 `none`：放行
- `Origin` 被 endpoint 的 CORS policy 信任：放行
- 其他 `Sec-Fetch-Site`：拒絕
- 沒有 `Sec-Fetch-Site` 但有 `Origin`：比較 `Origin` 是否等於 `scheme://host[:port]`
- 兩者都沒有：放行

重點：

- `Sec-Fetch-Site`/`Origin` 是瀏覽器控制的隱藏 header，JavaScript 不能偽造
- 這使得它能提供接近 token 的保證，但不需要 server-side state

### 3. 這個 middleware 不是直接擋掉請求，而是「記錄 verdict」

這一點很重要。

它不會直接擋掉所有非法 request，而是把結果寫進 `IAntiforgeryValidationFeature`，之後再由其他元件決定是否 400。

真正會讀這個 verdict 的地方包括：

- MVC antiforgery action
- Minimal API form-bound endpoint
- Blazor static SSR form posts
- 任何直接讀取 request form 的程式

也就是說：

- 如果 endpoint 從來不讀 form data，deny verdict 可能完全不會造成影響
- JSON endpoint 可能因為沒有 form binding 而不受影響

這代表：「封鎖是否發生」取決於是否有讀取 form 的程式，而不是單純看 middleware 有沒有跑。

### 4. 這會讓某些 endpoint 產生「看似被保護，但其實只在特定條件下」的感覺

例如：

- `MapPost` 讀取 JSON body：不會受到預設 CSRF 保護的行為變化
- 單純處理 `application/json` 的 API：本來就不容易被 browser form CSRF 利用

但這不代表完全安全：

- 如果你的 cookie-authenticated endpoint 接受 `application/x-www-form-urlencoded` 或 `text/plain`
- 你仍然要自己檢查 verdict，而不能假設 framework 已幫你處理好

---

## 三、跨站請求的實際影響與修正方式

### 1. 真正會壞掉的情境：不同 origin 的 browser-based form post

例如：

- SPA 在 `app.contoso.com`
- API 在 `api.contoso.com`
- 前端送出 form 表單到 API

此時 `Sec-Fetch-Site` 會是 `same-site`，不是 `same-origin`，因此觸發 deny，最後 form binding 可能直接 400。

### 2. 正確修法是 CORS，而不是關閉 CSRF

應該在 API 端設定明確信任來源：

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://app.contoso.com")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

注意：

- `AllowAnyOrigin` 不會被視為信任寫入來源
- 這是很好的設計，因為「任何人都能讀取」跟「任何人都能代表使用者修改資料」是不同層級的權限

### 3. `DisableCors` 也不是 opt-out

`[DisableCors]` 只會跳過 CORS 信任判斷，仍然要通過 `Sec-Fetch-Site` / `Origin` 檢查。

真正的 opt-out 是：

- Minimal API：`.DisableAntiforgery()`
- MVC：`[IgnoreAntiforgeryToken]`

這兩者都會寫入 `IAntiforgeryMetadata { RequiresValidation = false }`，讓 framework 知道不要驗證。

### 4. 如果你已經用 token 系統，行為沒變

如果你手動調用 `app.UseAntiforgery()`：

- token middleware 會在 CSRF middleware 之後跑
- 它會覆寫原本的 verdict
- 最終哪個結果勝出取決於 token 驗證結果

所以：

- 原本用 token 的 app 行為幾乎沒有變化
- 沒用 token 的 app，則會落到框架預設 verdict

---

## 四、Blazor SSR 的實際更動

Blazor static SSR 是少數會有真實行為變更的地方。

### 以前的狀態

- 如果 app 刪掉 `app.UseAntiforgery()`，可能會變成未受保護
- form 也可能還會輸出 antiforgery token

### .NET 11 後

- 即使你原本手動移除 antiforgery middleware，這個框架層級的 middleware 仍然會保護請求
- 渲染表單時也不再自動發 token，因為 token middleware 不存在了

這代表：

- 如果你原本刻意關掉 antiforgery，升級時要特別注意 breaking change
- 應該先看官方 migration note，再決定是否保留或重新啟用

---

## 五、Async validation：資料庫唯一性檢查可以搬到驗證層

.NET 11 讓資料驗證能夠支援 async。

### 新增能力

- `AsyncValidationAttribute`
- `IAsyncValidatableObject`
- `Validator.ValidateObjectAsync`

這表示可以把資料庫查詢放進 validation，而不是把它塞在 endpoint 最前幾行。

### 範例

```csharp
public class ReservationRequest : IAsyncValidatableObject
{
    [Required] public string Email { get; set; } = "";
    public DateOnly Date { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx) =>
        throw new InvalidOperationException(
            "ReservationRequest validates asynchronously. Use ValidateAsync.");

    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext ctx,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var calendar = ctx.GetRequiredService<IBookingCalendar>();
        if (!await calendar.IsOpenAsync(Date, ct))
            yield return new ValidationResult("Closed on that date.", [nameof(Date)]);
    }
}
```

### 重要注意：這是個尖銳邊界

- `IAsyncValidatableObject` 繼承自 `IValidatableObject`
- 所以你仍然必須實作同步的 `Validate()`
- 但 minimal API 驗證時，實際上只會呼叫 async 版本
- 如果同步版本回傳空集合，某些呼叫端會「靜默跳過」驗證，沒有任何錯誤

所以這裡的作法是：

- 讓同步版本直接 `throw`
- 在註解說明它是 async-only

這讓驗證邏輯更強大，但也要求開發者更加小心。

### 影響

這削弱了「只要用 FluentValidation 就沒問題」的通用答案。因為 attribute-based validation 現在也能做 async 檢查了。

---

## 六、其他三個較小但值得注意的改變

### 1. Kestrel HTTP/1.1 parser 改成不直接丟 `BadHttpRequestException`

原本 malformed request 會經歷異常流程，導致 stack unwinding 和 allocation。

現在改為回傳 result struct：

- success
- incomplete
- error

這有助於提高吞吐量，尤其在面對 scanning / hostile traffic 時更有效率。

### 2. ASP.NET Core 自動寫入 OpenTelemetry attributes

現在 HTTP server activity 會自動包含：

- method
- path
- status
- server address

這代表你可能不需要再額外掛 `OpenTelemetry.Instrumentation.AspNetCore`。

如果想關掉，可用：

```csharp
AppContext.SetSwitch("Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData", true);
```

### 3. OpenAPI 3.2 成為預設文件版本

如果下游工具（如 code generator、gateway、contract test）鎖版本到 3.0 / 3.1，升版前要先確認是否相容。

---

## 七、實際升級建議

### 1. 先在 preview 環境跑一次整套整合測試

特別注意：

- 跨 origin 的 form submit
- cookie-authenticated endpoint
- 接受 `x-www-form-urlencoded` / `text/plain` 的 API

### 2. 打開 debug logging

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Antiforgery.CsrfProtectionMiddleware": "Debug"
    }
  }
}
```

這可以幫你看到 `CsrfValidationFailed`。

### 3. 把所有需要保護的 endpoint 明確化

- 需要認證 + browser 可達的寫入端點：要特別確認保護
- 如果真的不需要 CSRF：明確使用 `.DisableAntiforgery()` 或 `[IgnoreAntiforgeryToken]`

### 4. 自己檢查 cookie-authenticated endpoints

尤其是接受以下內容類型的 API：

- `application/x-www-form-urlencoded`
- `text/plain`

這類端點最容易掉進「verdict 已記錄，但沒有地方真正 enforce」的灰色區域。

---

## 八、最終判斷

這篇文章的重點不是「CSRF 變得更難」而是：

- framework default 變成了 opinion
- `Program.cs` 不再完整代表實際行為
- 你必須閱讀框架預設，而不是假設它等價於你寫的程式碼

一句話總結：

> .NET 11 的 default 行為更安全，但也不等於「所有 endpoint 都被完全保護」。

這是最需要記住的升級觀念。

---

## 九、學習重點（速記）

- `Program.cs` 不再等於整個 pipeline
- .NET 11 會自動帶 CSRF middleware
- 它不是直接拒絕，而是記錄 verdict
- 真正的 400 依賴後續 form binding 或驗證判斷
- 跨站寫入要靠 CORS 明確授權，而不是 `AllowAnyOrigin`
- token-based app 基本不受影響
- async validation 讓資料庫驗證可以放在驗證層
- 升級前要特別檢查 form-based cookie API

