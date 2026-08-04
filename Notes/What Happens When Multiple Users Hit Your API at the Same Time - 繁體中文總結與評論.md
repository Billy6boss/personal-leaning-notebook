# 《當多位使用者同時打到你的 API 時，底層到底發生了什麼？》繁體中文總結與評論

> 原文：What Happens When Multiple Users Hit Your API at the Same Time?
> 原文背景：Spring Boot / Tomcat / Java
> 本筆記補充：**以 C# / ASP.NET Core / Kestrel 對照說明**，幫助熟悉 .NET 的工程師建立更精準的併發模型理解

---

## 📋 文章總結

這篇文章的核心觀點是：**多位使用者同時呼叫 API，並不是建立多份應用程式實例，而是由伺服器用多個執行緒並行處理請求；真正危險的，不是「同時很多 Request」，而是「多個執行緒共用同一份可變狀態」**。

以 Spring Boot 的世界來看，Tomcat 會從 Thread Pool 分派 Worker Thread 給每個 Request；而在 ASP.NET Core 的世界裡，雖然也同樣依賴 Thread Pool，但若你正確使用 `async`/`await` 與非同步 I/O，等待資料庫或網路 I/O 時，Thread 不一定會傻傻卡住。這也是理解「高併發 API 為什麼能撐住」與「為什麼某些程式在壓力下會壞掉」的分水嶺。

---

### 1. 同時很多人打 API，不是很多份 App 在跑

原文先破除一個常見誤解：當 100、1000 個使用者同時打 API，Spring Boot 不會複製出 100、1000 份應用程式。實際上，通常還是同一個 Process、同一份應用程式，只是伺服器把不同 Request 分配給不同執行緒處理。

在 Spring Boot 預設情境裡，這個角色通常是內嵌 Tomcat；在 ASP.NET Core 則可以對照為 Kestrel + .NET Thread Pool。你可以把它想像成同一間餐廳沒有複製出 100 間分店，而是同一間店裡有多個服務生同時接單。

**核心思想：高併發的本質不是複製應用程式，而是讓同一個應用程式安全地同時處理多個 Request。**

#### ❌ 錯誤理解

```csharp
// 錯誤心智模型：每個 Request 都有自己獨立的一套應用程式狀態
// 實際上，多數情況下是同一個 ASP.NET Core Process 在同時處理多個 Request。
```

#### ✅ 正確理解

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/hello", () => "Hello World");

app.Run();
```

上面這個 API 不會因為同時有很多人呼叫，就自動複製出很多份 `app`。真正同時被放大的，是「請求數量」與「執行中的工作數量」，不是應用程式實例數量。

---

### 2. Request 會被丟給執行緒處理，但 .NET 的 async 模型比文章講得更進一步

原文用 Tomcat 的 Thread-per-Request 模型做說明：每一個 Request 進來，Tomcat 從 Thread Pool 拿一條 Thread 出來處理它。這個模型在「同步阻塞式」的 Servlet / JDBC 情境下很好理解，因為 Request 等資料庫時，Thread 常常就跟著被卡住。

ASP.NET Core 也會使用 Thread Pool 處理 Request，但這裡有一個 .NET 工程師必須特別敏感的差異：如果你的 Controller / Minimal API / Service 使用 `async`/`await` 搭配真正的非同步 I/O（例如 EF Core 的 `ToListAsync()`、`HttpClient.SendAsync()`），那麼在等待 I/O 完成的期間，執行緒通常可以先回到 Thread Pool，去處理其他 Request。等 I/O 完成後，再由 I/O Completion Port 喚醒後續 continuation 繼續執行。

也就是說，**ASP.NET Core 並不只是「一個 Request 綁死一條 Thread 到結束」這麼單純**。如果你把同步阻塞程式碼硬搬進 ASP.NET Core，就會浪費它本來可用的伸縮性優勢。

**核心思想：在 ASP.NET Core 中，真正影響併發能力的，不只是 Thread Pool 大小，而是你有沒有把 I/O 寫成非同步。**

#### ❌ 同步阻塞寫法

```csharp
app.MapGet("/products", (AppDbContext db) =>
{
    // 同步查詢：等待資料庫時，執行緒會被卡住
    var products = db.Products.ToList();
    return Results.Ok(products);
});
```

#### ✅ 非同步 I/O 寫法

```csharp
app.MapGet("/products", async (AppDbContext db) =>
{
    // 非同步查詢：等待資料庫期間，執行緒可回到 Thread Pool
    var products = await db.Products.ToListAsync();
    return Results.Ok(products);
});
```

補一句實務判斷：`async` 不會讓 CPU-bound 工作自動變快；它主要改善的是「等待 I/O 時不要白白佔著執行緒」。

---

### 3. Spring Bean 的 Singleton，要對照 ASP.NET Core DI 的 Singleton 來看

原文很重要的一個提醒是：Spring 的 Controller / Service / Repository 預設都是 Singleton Bean，所以多個 Thread 會共用同一個物件實例。這也是為什麼把「使用者當前狀態」塞進 instance field 會爆炸。

在 ASP.NET Core，應該對照的是 DI 的 `ServiceLifetime.Singleton`，也就是透過 `builder.Services.AddSingleton<T>()` 註冊的服務。這類服務在整個應用程式生命週期中通常只會有一份實例，因此會被所有 Request 共用。

但有一個**非常值得點出的差異**：ASP.NET Core 的 Controller 類別本身預設不是 Singleton。一般 MVC / Web API Controller 在每次 Request 來時，會建立新的 Controller 實例，因此它的 instance field 行為上更接近「Request 私有」，不像 Spring MVC Controller 預設就是應用程式共用單例。這兩個框架若直接類比，很容易搞混。

**核心思想：在 ASP.NET Core，真正要小心共享狀態的是 `AddSingleton` 註冊的服務，不要把 Spring Controller 的 Singleton 心智模型直接套過來。**

#### ❌ 危險的共享 Singleton 狀態

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RequestContextCache>();

public class RequestContextCache
{
    public string? CurrentUserName { get; set; }
}
```

如果你把每次 Request 的使用者資訊塞進 `CurrentUserName`，不同 Request 就可能互相覆蓋。

#### ✅ 用正確 Lifetime 與 Request 內資料流

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<UserService>();

public class UserService
{
    public string BuildGreeting(string userName)
    {
        return $"Hello, {userName}";
    }
}
```

`UserService` 本身不持有可變共享狀態，只透過方法參數接收資料，這就是比較安全的做法。

---

### 4. Race Condition 的關鍵，不在 Singleton 本身，而在「可變共享欄位」

原文用一個很經典的 `counter++` 範例說明 Race Condition：兩條 Thread 同時讀到舊值，再各自加一後寫回，就會發生遺失更新（lost update）。

這個問題在 C# 一模一樣會發生。如果你在 `AddSingleton` 的 Service 裡放一個 `private int _counter`，然後直接做 `_counter++`，你不能期待它在高併發下還是正確遞增。因為 `++` 不是原子操作，它其實包含了讀取、計算、寫回三個步驟。

**核心思想：Singleton 可怕的不是「只有一份」，而是「很多執行緒同時改同一份可變資料」。**

#### ❌ Race Condition 範例

```csharp
public class CounterService
{
    private int _counter = 0;

    public int Increment()
    {
        return ++_counter;
    }
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CounterService>();
```

如果兩個執行緒同時執行：
- Thread A 讀到 `_counter = 10`
- Thread B 也讀到 `_counter = 10`
- A 寫回 11
- B 也寫回 11

你原本期待 11、12，結果卻變成 11、11。

#### ✅ 正確解法 1：`Interlocked.Increment`

```csharp
using System.Threading;

public class CounterService
{
    private int _counter = 0;

    public int Increment()
    {
        return Interlocked.Increment(ref _counter);
    }
}
```

#### ✅ 正確解法 2：`lock`

```csharp
public class CounterService
{
    private int _counter = 0;
    private readonly object _syncRoot = new();

    public int Increment()
    {
        lock (_syncRoot)
        {
            _counter++;
            return _counter;
        }
    }
}
```

如果用 Java 類比，這分別很接近 `AtomicInteger` 與 `synchronized` 的思路。

---

### 5. 區域變數通常是安全的，因為每個 Request 都有自己的執行內容

原文另一個很重要的教學點是：Local Variable（區域變數）通常不會有前面那種共享狀態問題。原因不是它「神奇地自動 thread-safe」，而是每次方法呼叫都會有自己獨立的執行內容；不同 Request 不會共用同一份區域變數。

對 ASP.NET Core 來說，不論是 Controller Action、Minimal API handler，或 Service method，只要資料是放在方法參數或區域變數裡，而不是塞進共享的 instance field，通常就安全得多。

**核心思想：優先把 Request 相關資料放在方法參數與區域變數，而不是物件欄位。**

#### ❌ 把 Request 狀態存在物件欄位

```csharp
public class PricingService
{
    private decimal _currentDiscount;

    public decimal Calculate(decimal price, decimal discount)
    {
        _currentDiscount = discount;
        return price - _currentDiscount;
    }
}
```

#### ✅ 用區域變數保存計算狀態

```csharp
public class PricingService
{
    public decimal Calculate(decimal price, decimal discount)
    {
        var currentDiscount = discount;
        return price - currentDiscount;
    }
}
```

前者的 `_currentDiscount` 如果被多個執行緒同時改動，結果就可能互相污染；後者的 `currentDiscount` 只活在這次方法呼叫裡。

---

### 6. Thread Pool 是有限資源，慢 Request 會直接壓垮吞吐量

原文提到一個實務上非常重要的現象：Thread Pool 不是無限的。當請求處理很慢，特別是同步阻塞在資料庫查詢、外部 API 或檔案 I/O 時，就會導致執行緒長時間被佔住。可用執行緒越來越少，後面進來的 Request 就只能等。

這個觀念在 ASP.NET Core 一樣成立。即使 .NET 有很成熟的 Thread Pool 調度機制，也不代表你可以把所有工作都寫成阻塞式，再期待框架替你兜底。高併發系統的伸縮性，本質上還是取決於：

1. 你的 Request 有多快結束
2. 你的 I/O 是否非同步
3. 你的資料庫連線池、HTTP 連線池是否健康
4. 你是否把不需要同步回傳給使用者的工作留在 Request Path 上

**核心思想：效能問題常常不是 CPU 不夠，而是執行緒與連線被慢 I/O 長時間占住。**

#### ❌ 在 Request 路徑上做長時間阻塞

```csharp
app.MapGet("/reports", () =>
{
    Thread.Sleep(5000);
    return Results.Ok("Report ready");
});
```

#### ✅ 優先縮短 Request 臨界路徑

```csharp
app.MapGet("/reports/{id}", async (int id, ReportService service) =>
{
    var report = await service.GetSummaryAsync(id);
    return Results.Ok(report);
});
```

真正需要數秒甚至數分鐘的工作，不應該硬塞在 API 同步回應流程裡。

---

### 7. 耗時工作要搬去背景處理，但 ASP.NET Core 要避開 fire-and-forget 地雷

原文提到 Spring 的 `@Async`：把寄信、產報表、通知等耗時工作丟到另一個 Thread Pool，讓使用者先拿到回應。這個方向是對的，但如果搬到 .NET，不能只停在「用 `Task.Run` 丟出去就好」的層次。

在 ASP.NET Core 裡，常見對照方式有三種：

1. 短小、可容忍丟失的工作：有限度地用 `Task.Run`
2. 應用程式內背景工作：`IHostedService` / `BackgroundService`
3. 真正重要、需要可靠交付的工作：Queue-based background task / Message Queue

原因很簡單：**fire-and-forget Task 如果沒有被追蹤，拋出的例外可能不會進入你原本期待的錯誤處理流程；應用程式關閉、重啟或回收時，這些工作也可能直接中斷。** 這是很多 ASP.NET Core 專案早期很常踩到的坑。

**核心思想：把耗時工作移出 Request Path 沒錯，但「丟到背景」不等於「可靠完成」。**

#### ❌ 直接 fire-and-forget

```csharp
app.MapPost("/orders", async (Order order, EmailService emailService) =>
{
    _ = Task.Run(() => emailService.SendOrderCreatedEmailAsync(order));
    return Results.Accepted();
});
```

這樣做的問題包括：
- 例外可能沒有被妥善追蹤
- App 關閉時工作可能中斷
- 若誤用 scoped service，還可能踩到物件生命週期問題

#### ✅ 用 `BackgroundService` 或佇列式背景工作

```csharp
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);
}

app.MapPost("/orders", async (Order order, IBackgroundTaskQueue queue) =>
{
    await queue.QueueAsync(async cancellationToken =>
    {
        await Task.Delay(100, cancellationToken);
        // 實務上可在這裡做寄信、通知、事件發布等工作
    });

    return Results.Accepted();
});
```

如果是更關鍵的業務流程，例如付款後通知、庫存同步、跨系統事件分發，通常應該再往 Message Queue / Broker（如 Azure Service Bus、RabbitMQ、Kafka）走，而不是只靠 Process 內背景執行。

---

### 8. 最佳實踐的真正重點是「讓共享狀態最小化、讓 Request 儘快釋放資源」

原文最後給的最佳實踐其實很務實：Service 保持無狀態、使用建構子注入、優先使用區域變數、保持 API 快速回應、使用連線池、耗時工作非同步化。這些原則如果翻成 .NET 的語境，可以進一步整理成下面幾句：

- `AddSingleton` 服務預設視為**可被多執行緒同時呼叫**
- 不要在 Singleton 裡存放 Request 專屬狀態
- I/O 優先使用 `async`/`await` + 真正的非同步 API
- 避免同步阻塞資料庫、HTTP、檔案系統
- 背景工作要有生命週期管理與錯誤追蹤
- 連線池（DB / HTTP）與 Thread Pool 一樣，都是有限資源

**核心思想：高併發程式設計的本質不是炫技，而是誠實地管理共享狀態與有限資源。**

#### ❌ 有狀態 Singleton

```csharp
public class CheckoutService
{
    private string? _currentOrderId;

    public Task ProcessAsync(string orderId)
    {
        _currentOrderId = orderId;
        return Task.CompletedTask;
    }
}
```

#### ✅ 無狀態 Service + 建構子注入

```csharp
public class CheckoutService
{
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(ILogger<CheckoutService> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(string orderId)
    {
        _logger.LogInformation("Processing order | orderId={OrderId}", orderId);
        return Task.CompletedTask;
    }
}
```

---

## 🎯 資深工程師評論

### 整體評價

這篇文章非常適合拿來當「第一次理解 Web API 併發模型」的入門教材。它用餐廳服務生的比喻，把 Thread Pool、Singleton Bean、Race Condition、慢查詢塞滿執行緒池這幾件事講得相當直覺。對初學 Spring Boot 或剛接觸後端併發觀念的人來說，這篇的教學價值很高。

但如果讀者是已經做過 C# / .NET Web API、也碰過生產環境效能議題的工程師，就必須知道：**這篇文章講的是一個「同步 Servlet 心智模型下的基礎版本」，它有用，但不夠完整。**

---

### 值得肯定的地方

**第一，作者有把「共享 Bean」和「共享狀態」的危險講清楚。** 很多初學者以為只要 Controller / Service 能跑就好，卻沒有意識到 `private field` 一旦放進 Singleton，就等於讓所有 Request 共用同一塊可變記憶體。這個提醒非常重要，而且是會直接造成正式環境 Bug 的那種重要。

**第二，作者沒有把 thread safety 講成玄學，而是用 `counter++` 這種最低成本的例子說明 race condition。** 這對教學非常有效，因為大家都看得懂，也很容易想像錯誤是怎麼發生的。

**第三，作者把「慢 API 會吃掉 Thread Pool」這件事點出來了。** 這雖然是基本觀念，但很多團隊在壓測出問題前，根本不會把「慢查詢」和「吞吐量下降」直接連起來。

---

### 可以更深入的地方

**最重要的補充是：文章完全沒有觸及 ASP.NET Core / Kestrel 這類現代非同步 Web 框架的 I/O 模型差異。**

原文的敘事幾乎等同於：Request 進來 → 綁住一條 Thread → 等資料庫 → Thread 一直卡住直到回應完成。這在傳統同步阻塞式的 Tomcat + JDBC 心智模型裡是成立的；但在 ASP.NET Core + Kestrel 中，如果你使用 `async`/`await` 配合 EF Core 的 `ToListAsync()`、`SaveChangesAsync()` 或 `HttpClient.SendAsync()`，那麼 Request 在等待 I/O 回來時，**執行緒通常會先釋放回 .NET Thread Pool**，由 I/O Completion Port 完成後續喚醒。這代表：

1. ASP.NET Core 不是單純的「一個 Request 全程霸佔一條 Thread」
2. 「同步 I/O」和「非同步 I/O」在高併發下的伸縮性差異，會非常大
3. 真正的瓶頸分析不能只看 Thread Pool，還要看 DB 連線池、外部依賴延遲、非同步 API 使用是否正確

這是本篇筆記最重要的 .NET 對照補充，也是原文若想跨框架成立，最需要被補上的一塊。

**第二個可再加深的點，是 Spring Controller 與 ASP.NET Core Controller 的生命週期差異。** 原文把 Controller 也放進 Singleton Bean 的敘事中，但 ASP.NET Core 的 Controller 預設不是 Singleton。若讀者直接把這個結論搬進 .NET，會產生錯誤心智模型。真正該警戒的是 `AddSingleton<T>()` 註冊的服務，不是每個 Controller instance field 都一定跨 Request 共用。

**第三個可以補強的點，是背景工作可靠性。** 原文提到 `@Async` 的方向是正確的，但在 .NET 實務上，若只用 fire-and-forget `Task.Run`，很容易把例外追蹤、取消、應用程式關閉收尾等問題全部略過。更穩健的做法通常是 `BackgroundService`、佇列式背景任務，甚至外部 Message Queue。

---

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐☆☆ |
| 新穎性 | ⭐⭐☆☆☆ |
| 適合對象 | 剛接觸 Web API 併發、Spring Boot 初中階工程師；亦適合 .NET 工程師拿來建立基礎對照模型 |

**總結一句話：這篇文章很適合拿來建立「共享狀態會出事」的第一層直覺，但若你正在寫 ASP.NET Core，高併發下真正的關鍵差異其實是同步阻塞 I/O 與 `async` 非同步 I/O 的模型分野。**

---

## 延伸閱讀

- *Concurrency in C# Cookbook* — Stephen Cleary
- *ASP.NET Core in Action* — Andrew Lock
- Microsoft Learn：Asynchronous programming with async and await  
  https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/
- Microsoft Learn：Fundamentals of ASP.NET Core request processing  
  https://learn.microsoft.com/aspnet/core/fundamentals/middleware/
- Microsoft Learn：Use scoped services within a `BackgroundService`  
  https://learn.microsoft.com/dotnet/core/extensions/scoped-service
- Microsoft Learn：EF Core async query and save operations  
  https://learn.microsoft.com/ef/core/miscellaneous/async
