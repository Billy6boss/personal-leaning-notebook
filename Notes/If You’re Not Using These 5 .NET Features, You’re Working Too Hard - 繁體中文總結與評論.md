# 《如果你還沒在用這 5 個 .NET 功能，你其實做得太辛苦了》繁體中文總結與評論

> 原文：If You’re Not Using These 5 .NET Features, You’re Working Too Hard

---

## 📋 文章總結

這篇文章的核心觀點是：**.NET 其實早就內建了很多能解決「平台判斷、Plugin 載入、版本資訊、DI 建構」等實務痛點的 API，只是大多數人還在沿用較舊、較笨重、也較容易出錯的寫法。** 這不是在追求冷門知識，而是在提醒你：當系統進入跨平台、外掛化、部署診斷與框架整合階段時，善用框架原生能力，往往比自己手刻 workaround 更穩、更清楚，也更容易維護。

---

### 1. 用 `OperatingSystem.IsWindows()` 取代冗長的 `RuntimeInformation` 寫法

以前在 .NET 裡做平台判斷，很多人第一反應還是：

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Windows-specific code
}
```

這不是不能用，但問題是太冗，而且當條件一多時，可讀性很快就下降。現在更直接的寫法是：

```csharp
if (OperatingSystem.IsWindows())
{
    Console.WriteLine("Running on Windows");
}
```

同理也有 `OperatingSystem.IsLinux()`、`OperatingSystem.IsMacOS()` 等 API。這類方法的價值不只是「少打幾個字」，而是**把平台意圖直接寫在程式碼表面**。讀程式的人不需要先理解 `OSPlatform` enum，也不用在腦中做語意轉譯。

#### ❌ 舊寫法

```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    StartWindowsService();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    StartSystemdService();
}
```

#### ✅ 新寫法

```csharp
if (OperatingSystem.IsWindows())
{
    StartWindowsService();
}
else if (OperatingSystem.IsLinux())
{
    StartSystemdService();
}
```

**核心思想：平台判斷不是低階細節，而是業務意圖的一部分，應該寫得讓人一眼看懂。**

---

### 2. 用 `AssemblyLoadContext` 做 Plugin 隔離，別再把所有 DLL 丟進同一個世界

只要做過 Plugin 系統，大多數人都踩過同一種雷：Host 程式用了某個版本的 DLL，Plugin A 依賴另一個版本，Plugin B 又依賴第三個版本，最後整個載入流程變成大型碰撞現場。

`AssemblyLoadContext` 的價值就在這裡。它讓你為每個 Plugin 建立自己的組件載入脈絡，讓不同 Plugin 可以各自依賴不同版本的相同 DLL，而不會互相污染。

```csharp
using System.Reflection;
using System.Runtime.Loader;

public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath)
        : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is not null
            ? LoadFromAssemblyPath(assemblyPath)
            : null;
    }
}
```

這種寫法比過去「把 DLL 複製到某個資料夾，然後祈禱 CLR 幫你挑到對的版本」成熟太多。對於長時間執行的服務、桌面應用程式、規則引擎、擴充模組平台來說，這是從「能跑」走向「可維運」的分水嶺。

**核心思想：Plugin 系統的本質不是載入 DLL，而是管理隔離邊界。**

---

### 3. `AssemblyDependencyResolver`：Plugin 相依性解析的真正關鍵角色

很多人第一次看到 `AssemblyLoadContext` 範例時，會把焦點全部放在 `Load()` override 上，但真正讓這件事變得可靠的，其實是 `AssemblyDependencyResolver`。

```csharp
var resolver = new AssemblyDependencyResolver(pluginPath);
var path = resolver.ResolveAssemblyToPath(assemblyName);
```

它的角色很單純：給它一個 Plugin 主組件路徑，它會根據 .NET 的相依性資訊，幫你找出某個組件名稱應該實際載到哪個檔案。這代表你不需要自己寫一堆脆弱規則，例如：

- 先找目前資料夾
- 找不到再找某個 plugins 資料夾
- 再不行就去共用目錄
- 最後還得自己處理版本衝突

有了 `AssemblyDependencyResolver`，這些解析責任就交回給 .NET 既有的相依性模型處理。尤其當 Host 用 `Newtonsoft.Json` 12.x、Plugin 用 13.x 時，這個 API 幾乎就是避免災難的必要零件。

```csharp
protected override Assembly? Load(AssemblyName assemblyName)
{
    var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
    if (assemblyPath is null)
    {
        return null;
    }

    return LoadFromAssemblyPath(assemblyPath);
}
```

**核心思想：自訂載入器最困難的從來不是「載入」，而是「正確解析相依性」。**

---

### 4. 正確理解組件版本：`AssemblyName.Version`、`FileVersion`、`InformationalVersion` 不是同一件事

很多程式碼都寫過這種版本讀取方式：

```csharp
var version = Assembly.GetExecutingAssembly().GetName().Version;
```

看起來合理，但實際上這只回答了「這個組件的編譯期 Assembly Version 是多少」，不代表它就是你想顯示給使用者看的版本字串。

這裡至少有三種常見版本資訊：

1. `AssemblyName.Version`：組件識別用的版本，偏向 CLR/組件繫結語意
2. `FileVersionInfo.FileVersion`：檔案版本，偏向 Windows 檔案屬性與部署資訊
3. `AssemblyInformationalVersionAttribute`：對外溝通的版本字串，通常最接近語意化版本

#### 1) 取得 Assembly Version

```csharp
var assemblyVersion = Assembly
    .GetExecutingAssembly()
    .GetName()
    .Version;
```

#### 2) 取得 File Version

```csharp
using System.Diagnostics;
using System.Reflection;

var assembly = Assembly.GetExecutingAssembly();
var fileVersion = FileVersionInfo
    .GetVersionInfo(assembly.Location)
    .FileVersion;
```

#### 3) 取得 Informational Version

```csharp
using System.Reflection;

var informationalVersion = Assembly
    .GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion;
```

如果你要在 API response、診斷頁面、About 畫面、健康檢查端點顯示「使用者看得懂」的版本，多半應優先考慮 `AssemblyInformationalVersionAttribute`。但也要注意一個常見實務地雷：它常常會被 MSBuild 或 CI/CD 流程塞入 build metadata，例如：

```text
1.0.0+a1b2c3d
```

如果你只想顯示純粹的語意化版本號，而不想把 git hash 一起露出去，通常要自行處理：

```csharp
var displayVersion = informationalVersion?.Split('+')[0];
```

**核心思想：版本號不是單一概念，先定義你要回答的是「組件識別」、「檔案資訊」還是「產品版本」。**

---

### 5. `ActivatorUtilities.CreateInstance<T>`：讓 DI 幫你建沒註冊的物件

DI 用久了之後，常常會遇到一種尷尬情境：某個型別本身不想正式註冊進容器，但它的 constructor 依賴的服務都已經在容器裡。這時很多人會手動把每個 dependency resolve 出來，再自己 `new` 一次，結果把原本該由容器處理的事情又搬回手上。

`ActivatorUtilities.CreateInstance<T>(serviceProvider)` 就是這種情境的標準解法：

```csharp
var myService = ActivatorUtilities.CreateInstance<MyService>(serviceProvider);
```

它的意思是：**這個型別本身沒有註冊沒關係，但請幫我把它 constructor 需要的相依物件從容器裡補齊。**

例如：

```csharp
public sealed class ReportGenerator
{
    private readonly ILogger<ReportGenerator> _logger;
    private readonly IClock _clock;

    public ReportGenerator(ILogger<ReportGenerator> logger, IClock clock)
    {
        _logger = logger;
        _clock = clock;
    }

    public string Generate(string reportName)
    {
        _logger.LogInformation(
            "Generating report | reportName={ReportName} | generatedAt={GeneratedAt}",
            reportName,
            _clock.UtcNow);

        return $"{reportName}-{_clock.UtcNow:yyyyMMddHHmmss}";
    }
}

var generator = ActivatorUtilities.CreateInstance<ReportGenerator>(serviceProvider);
var reportId = generator.Generate("SalesSummary");
```

這個 API 不只是偶爾救急的小工具。事實上，你每天寫 ASP.NET Core 時，很可能早就在「間接使用」它了：Controller、Razor Page、Minimal API 某些參數與 framework 內部 activator 機制，背後就大量依賴這類模式來建構物件。

**核心思想：不是所有物件都要註冊進容器，但相依注入的建構流程仍然應交給容器負責。**

---

## 🎯 資深工程師評論

### 整體評價

這篇文章選題很務實，挑的五個點都不是「新功能介紹」，而是**那些在日常開發中很少被主動提起，但一旦碰到特定場景就能大幅降低痛苦指數的 API**。尤其第 2 到第 4 點，明顯已經不是入門 C# 教學會處理的層次，而是開始碰到跨平台部署、外掛架構、版本可觀測性之後，才會意識到的重要細節。

文章最大的優點是節奏很好：每一點都先從「你原本可能怎麼做」切入，再介紹 .NET 已經提供的更好做法。這讓內容不會像冷知識列表，而比較像是實務經驗的壓縮版。

---

### 值得肯定的地方

**第 2、3 點是全文最有價值的部分。** 很多人知道 .NET 可以做 Plugin，但不知道真正能把 Plugin 系統做穩的關鍵不是反射，而是 `AssemblyLoadContext` + `AssemblyDependencyResolver` 這組合。這兩個 API 解決的不是「怎麼把 DLL 載進來」，而是更實際的問題：**怎麼讓 Host 與多個 Plugin 在依賴版本不同的情況下，仍然能和平共處。**

**第 4 點談版本資訊也很實用。** 很多系統的 `/version` endpoint、About 頁面、log 啟動訊息，其實都長期混用不同版本來源，導致顯示出來的值和產品經理、SRE、部署流程認知中的版本不是同一件事。文章願意把 `AssemblyName.Version`、`FileVersion`、`InformationalVersion` 分開說明，這是對實務很有幫助的整理。

**第 5 點則點出一個典型的 DI 成熟度差異。** 初學者遇到容器外物件，常常第一時間就退回手動建構；較熟悉 ASP.NET Core 生態系的工程師則會知道，容器不只負責「註冊後 resolve」，也提供了像 `ActivatorUtilities` 這種更彈性的 activator 能力。

---

### 可以更深入的地方

**1. `OperatingSystem.IsWindows()` 這類 API 的價值，不只在於可讀性，還在於它們和平台相容性分析器是整合的。**

這些 API 背後帶有 `[SupportedOSPlatformGuard]` 一類的 attribute，讓編譯器與 Roslyn analyzer 能理解：「如果你先判斷 `OperatingSystem.IsWindows()` 為 true，後面的區塊就可以安全呼叫 Windows-only API。」這個能力在寫跨平台 library、NativeAOT、Trimming 友善程式碼時很重要，因為它讓**平台守門條件可以被靜態驗證**，而不只是靠人類約定。換句話說，這不是單純語法糖，而是讓平台特定程式碼更容易被工具鏈理解。

**2. `AssemblyInformationalVersion` 在 CI/CD 環境中常常不是你以為的「乾淨版本號」。**

很多專案會自動把 git commit hash 或 build metadata 加在 `+` 後面，例如 `1.0.0+a1b2c3d`。這對追部署來源很有幫助，但如果你把它直接顯示在 UI、通知訊息或 API response，可能會比你預期中更冗。實務上通常要先決定用途：如果是診斷頁面，就保留完整字串；如果是顯示給一般使用者看，就取 `Split('+')[0]` 後的主版本號即可。**同一個版本來源，顯示層與診斷層不一定要長一樣。**

**3. `ActivatorUtilities.CreateInstance` 其實不是冷門 API，而是你每天都在間接使用的框架基礎設施。**

ASP.NET Core 內部在建立 Controller、PageModel、某些 filter、甚至 Minimal API 的部分參數與 delegate 執行物件時，本質上都依賴類似的 activator 機制。也就是說，這個 API 不是某種邊角功能，而是整個 Microsoft.Extensions.DependencyInjection 生態系裡非常核心的一塊。把它理解成「容器版的智慧型 `new`」會比把它當成小技巧更貼近真相。

**4. `AssemblyLoadContext` 一旦進入 `Unload()` 場景，真正困難的不是呼叫卸載，而是確保真的能被回收。**

很多人以為 `context.Unload()` 呼叫完就結束了，但 collectible `AssemblyLoadContext` 是否真的被 GC 回收，取決於**還有沒有任何活著的參考鏈指向該 Context 載入出的型別或物件**。常見地雷包括：

- Host 端還留著 Plugin instance 的強參考
- 某個 Delegate 被快取在靜態欄位裡
- Plugin 訂閱了 Host 事件卻沒有取消訂閱
- 執行中的背景工作仍持有該型別實例

只要這些東西還存在，Unload 幾乎等於沒卸。實務上常要配合 `WeakReference` 驗證是否真的釋放，並仔細管理事件訂閱、跨 Context 傳遞的物件形狀，以及 Plugin 生命週期。**可卸載 Plugin 的難點從來不是 API 不夠，而是參考關係太容易漏。**

---

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐⭐☆ |
| 新穎性 | ⭐⭐⭐☆☆ |
| 適合對象 | 已熟悉 ASP.NET Core / .NET 基礎、開始碰部署與框架整合的中高階工程師 |

**這篇文章很適合作為「從會用 .NET 到用得更像 .NET」的過渡讀物。** 它談的不是語法炫技，而是框架內建能力的正確打開方式。若想延伸深入，可以參考：

- Microsoft Learn: [AssemblyLoadContext class](https://learn.microsoft.com/dotnet/api/system.runtime.loader.assemblyloadcontext)
- Microsoft Learn: [Create a .NET Core application with plugins](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support)
- Microsoft Learn: [AssemblyDependencyResolver class](https://learn.microsoft.com/dotnet/api/system.runtime.loader.assemblydependencyresolver)
- Microsoft Learn: [AssemblyInformationalVersionAttribute class](https://learn.microsoft.com/dotnet/api/system.reflection.assemblyinformationalversionattribute)
- Microsoft Learn: [ActivatorUtilities Class](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.activatorutilities)
- 書籍：*Pro .NET Memory Management* — Konrad Kokosa
- 書籍：*CLR via C#* — Jeffrey Richter
