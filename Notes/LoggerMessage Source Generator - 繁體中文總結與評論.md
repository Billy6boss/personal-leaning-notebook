# 《Stop Using _logger.LogInformation() in C# Hot Paths》繁體中文總結與評論

> 原文：Stop Using _logger.LogInformation() in C# Hot Paths — How LoggerMessage source generators reduce allocations, improve performance, and keep your .NET logs clean.

---

## 📋 文章總結

這篇文章的核心論點是：在 .NET 的高頻路徑（Hot Path）中，傳統的 `_logger.LogInformation(...)` 看起來無害，但每次呼叫都會產生隱藏的 heap allocation、value type boxing 與 `object[]` 陣列配置，在高流量情況下會累積成明顯的 GC 壓力。

解法不是不寫 Log，而是改用 .NET 6 引入的 **LoggerMessage Source Generator**——透過 `[LoggerMessage]` attribute 搭配 `partial method`，讓 Roslyn 在編譯期產生零 boxing、零 runtime parsing 的強型別 Log 方法，同時帶來更好的可維護性與型別安全。

---

### 1. 傳統日誌的隱藏成本（The Hidden Cost of Traditional Logging）

每次呼叫 `_logger.LogDebug("...", originId, dataSourceId)` 時，執行期會：
1. **Boxing value types**：`Guid`、`int` 等 value type 必須被裝箱成 `object` 才能放入參數
2. **配置 `object[]` 陣列**：所有參數會被打包成一個 heap 上的陣列
3. **Runtime 解析 message template**：每次執行都重新解析格式字串

這三件事在 cache lookup、middleware pipeline、background worker 等每秒執行千次的路徑上，會累積成大量短命物件，頻繁觸發 GC。

**核心思想**：Log 不是免費的，在 Hot Path 上，一行「無害」的 LogDebug 可以是你的效能瓶頸。

#### ❌ 傳統寫法（每次呼叫都有隱藏 allocation）

```csharp
// 每次執行：boxing Guid、配置 object[]、runtime 解析 template
_logger.LogDebug(
    "Cache L1 hit for OriginId={OriginId}, DataSourceId={DataSourceId}, CorrelationId={CorrelationId}",
    originId,      // Guid → 被 boxing 成 object
    dataSourceId,  // string（不 boxing，但仍進 object[]）
    correlationId);
```

---

### 2. LoggerMessage Source Generator 解法（The Source Generator Solution）

在 `static partial class` 中定義 `partial` 方法，標上 `[LoggerMessage]` attribute。Roslyn 編譯時會自動產生底層實作：**強型別參數（無 boxing）、無 `object[]`、template 解析只做一次**。

**核心思想**：把 Log 的模板解析與參數配置移到編譯期，執行期只做最少的事。

#### ✅ Source Generator 寫法

```csharp
// 定義在獨立的 static partial class 中（建議依 Domain 分檔）
public static partial class QuestionnaireCacheLog
{
    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Debug,
        Message = "Cache L1 hit for OriginId={OriginId}, DataSourceId={DataSourceId}, CorrelationId={CorrelationId}")]
    public static partial void L1Hit(
        ILogger logger,
        Guid originId,       // 強型別，編譯產生的程式碼不需要 boxing
        string dataSourceId,
        string correlationId);
}

// 使用端：乾淨、強型別
QuestionnaireCacheLog.L1Hit(_logger, originId, dataSourceId, correlationId);
```

#### ⚠️ 重要補充：昂貴的參數仍需 guard

Source Generator 解決了 Log 方法本身的 allocation，但**你傳進去的參數表達式仍會被執行**。如果參數需要呼叫方法或建構物件，仍要用 `IsEnabled` 保護：

```csharp
// 若 BuildExpensiveCorrelationId() 代價高，不加 guard 的話
// 即使 Debug level 被關閉，這個方法仍然會被執行
if (_logger.IsEnabled(LogLevel.Debug))
{
    QuestionnaireCacheLog.L1Hit(
        _logger,
        originId,
        dataSourceId,
        BuildExpensiveCorrelationId()); // 只在 Debug 開啟時才執行
}
```

---

### 3. 開發體驗的四個改善（4 Developer Experience Wins）

除了效能，Source Generator 也帶來可維護性的提升：

**核心思想**：Log 是可觀測性的基礎建設，它應該和程式碼一樣有型別安全與集中管理。

#### 3-1. 編譯期型別安全

```csharp
// ❌ 傳統寫法：參數順序錯誤要到執行期才發現，或根本不會出錯只是語意錯
_logger.LogDebug("User {UserId} not found", userName); // UserId 放了 userName 的值

// ✅ Source Generator：方法簽章明確，傳錯型別編譯就報錯
public static partial class UserLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "User {UserId} not found")]
    public static partial void UserNotFound(ILogger logger, Guid userId); // 只接受 Guid
}

UserLog.UserNotFound(_logger, userId); // 傳 userName (string) → 編譯錯誤
```

#### 3-2. 集中管理 EventId

```csharp
// ❌ EventId 散落各處，容易重複、難以審計
_logger.LogInformation(new EventId(3101), "...");  // 在 ServiceA
_logger.LogDebug(new EventId(3101), "...");         // 在 ServiceB，EventId 重複了！

// ✅ 集中在 static class，一眼看出所有 EventId，IDE 也能找重複
public static partial class QuestionnaireCacheLog
{
    // EventId 3101 = L1 Hit
    [LoggerMessage(EventId = 3101, Level = LogLevel.Debug, Message = "...")]
    public static partial void L1Hit(ILogger logger, Guid originId, string dataSourceId, string correlationId);

    // EventId 3102 = L1 Miss
    [LoggerMessage(EventId = 3102, Level = LogLevel.Debug, Message = "Cache L1 miss for OriginId={OriginId}")]
    public static partial void L1Miss(ILogger logger, Guid originId);
}
```

#### 3-3. 業務邏輯不被 Log 雜訊汙染

```csharp
// ❌ 業務方法夾雜多行 Log 模板，可讀性差
public async Task ProcessOrderAsync(Order order)
{
    _logger.LogDebug(
        "Processing order {OrderId} for customer {CustomerId} with {ItemCount} items at {Timestamp}",
        order.Id, order.CustomerId, order.Items.Count, DateTimeOffset.UtcNow);

    // 實際業務邏輯...

    _logger.LogInformation("Order {OrderId} processed successfully in {ElapsedMs}ms", order.Id, elapsed);
}

// ✅ 業務方法只關注業務邏輯，Log 定義在別處
public async Task ProcessOrderAsync(Order order)
{
    OrderLog.ProcessingStarted(_logger, order.Id, order.CustomerId, order.Items.Count);

    // 實際業務邏輯...

    OrderLog.ProcessingCompleted(_logger, order.Id, elapsed);
}
```

---

### 4. 什麼時候用，什麼時候不用（When to Use）

不需要一次把整個專案的 Log 全部重寫，聚焦在真正有影響的地方：

| 情境 | 建議 |
|------|------|
| Cache Hit/Miss 記錄 | ✅ 一定用 Source Generator |
| HTTP Middleware Pipeline（每個 request 都跑） | ✅ 一定用 Source Generator |
| Background Worker 處理大量訊息 | ✅ 一定用 Source Generator |
| 高 SLA API（GC pause 會直接影響 p99 latency） | ✅ 一定用 Source Generator |
| 啟動/關機時的設定 Log | ❌ 傳統寫法即可 |
| Global Exception Handler 裡的錯誤 Log | ❌ 傳統寫法即可（極少觸發） |
| 本地暫時性 Debug Log | ❌ 傳統寫法即可（本來就要刪） |

---

## 🎯 專業 C#/.NET 資深工程師評論

### 整體評價

這篇文章點出的問題是真實存在的，也是很多 .NET 開發者根本不知道自己踩到的坑。在 GC 壓力較高的服務中，Hot Path 上的 `LogDebug` 確實是你 PerfView 或 dotMemory 跑出來會看到的東西之一。

文章的定位是「入門到 Source Generator」的實用指南，它做到了。程式碼範例清晰、反例和正例的對比直接。對於還在用傳統寫法的 .NET 開發者，這篇文章的 ROI 很高——讀完、照做，Hot Path 的 allocation 馬上有感。

### 值得肯定的地方

**「昂貴參數仍需 `IsEnabled` guard」這個補充非常關鍵**，很多教學文都漏掉這一點。Source Generator 只解決 Log 方法本身的 overhead，不能幫你省掉傳入參數的計算成本。這個細節在實務中常被忽略，文章有特別標出來值得肯定。

**集中管理 EventId 的觀點在企業級系統中特別有價值**。當你的服務接 Application Insights、Datadog 或 ELK，EventId 是你做 alert 和 query 的依據。EventId 重複或散落各處，是維運上的隱形地雷，這個 pattern 把它解決掉。

**把 Log 定義移出業務方法的做法**，除了效能，更重要的是讓業務邏輯的訊號雜訊比提高。在做 Code Review 時，一個方法如果有一半都是 Log template，很難快速看懂業務流程。

### 可以更深入的地方

**文章沒有提到 `LogLevel` 的動態調整情境**。在生產環境，很多團隊會透過 `appsettings` 或 Feature Flag 動態把特定 namespace 的 Log level 從 `Warning` 改成 `Debug`。Source Generator 在這個情境下的行為和傳統寫法一致（都依賴 `IsEnabled`），但文章沒有說明，初學者可能以為 Source Generator 會「提前把 Debug level 的 Log 編譯掉」。

**文章推薦的工具清單（Serilog、OpenTelemetry、Scrutor）值得更多說明**。特別是 **OpenTelemetry 搭配 `Meter` API** 這個角度——如果你 Log 某件事只是為了「計次」（例如 cache hit rate），那根本不應該用 Log，應該用 `System.Diagnostics.Metrics.Counter<T>`。Log 是給人看的事件記錄，Metrics 是給機器彙總的數值，兩者的用途不同，混用是效能和可觀測性設計上的常見錯誤。

**效能數字的情境說明不夠完整**。文章有提到 benchmark 結果，但沒有說明測試情境下 Log sink 是否真的有寫出去（通常 benchmark 會把 sink 設成 NullLogger 或 disabled）。在真實情境中，Log 的瓶頸往往不是 allocation，而是 I/O（寫檔案、發送到遠端 sink）。Source Generator 減少的是 CPU 和 GC 的成本，不是 I/O 成本。

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐☆☆ |
| 新穎性 | ⭐⭐⭐☆☆ |
| 適合對象 | 初階至中階 .NET 工程師 |

**延伸閱讀**：
- [Microsoft Docs — Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)（官方文件，有完整 API 說明與生成程式碼範例）
- [BenchmarkDotNet 官網](https://benchmarkdotnet.org/)（學會自己量才知道哪裡真的慢，不要靠感覺）
- *Pro .NET Memory Management* — Konrad Kokosa（理解 GC、allocation、boxing 的根本，這篇文章的問題都有更完整的解釋）
- [dotnet/diagnostics — PerfView](https://github.com/microsoft/perfview)（學會用 PerfView 找 allocation hotspot，這是找到「你的 Log 真的在浪費資源」的實際工具）
