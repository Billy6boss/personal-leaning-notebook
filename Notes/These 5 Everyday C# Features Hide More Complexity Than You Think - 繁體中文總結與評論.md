# 《C# 中這 5 個日常功能，藏著比你想的更多複雜度》繁體中文總結與評論

> 原文：These 5 Everyday C# Features Hide More Complexity Than You Think

---

## 📋 文章總結

文章的核心論點很簡單：**「熟悉」不等於「理解」**。寫 C# 久了之後，像 expression-bodied member、`readonly`、`default`、`Span<T>` 這些功能會變成肌肉記憶，寫的時候根本不會多想。但正是這些「每天都在用、卻很少細想」的功能，往往在你設計 Public API、追效能問題、或被編譯器拒絕一段看起來很合理的程式碼時，才會讓你發現自己其實一知半解。

以下逐一拆解文章提到的 5 個功能。

---

### 1️⃣ Expression-Bodied Members：好讀，但別走火入魔

Expression-bodied members（`=>` 語法）能拿掉方法、屬性、建構子的儀式感，讓「只是回傳一個值」的成員更簡潔：

```csharp
public string FullName => $"{FirstName} {LastName}";
public decimal CalculateTax(decimal amount) => amount * TaxRate;
```

這種寫法本身沒問題，問題出在**工程師迷戀語法本身，而不是可讀性**。例如：

```csharp
// 已經接近「不值得用 => 」的臨界點
public async Task<Order> GetOrderAsync(int id) =>
    await _repository.GetAsync(id);
```

一旦之後要加 log、驗證、例外處理、埋 metrics，這個方法馬上得改回大括號寫法，變成不斷來回改寫。更糟的是硬把條件邏輯塞進單一運算式：

```csharp
// 編譯器沒意見，你的同事會有意見
public string GetStatus() =>
    IsActive
        ? HasSubscription
            ? "Premium"
            : "Free"
        : "Inactive";
```

**判斷準則**：只有當成員「真的就是一個運算式、意圖一眼可見」時才用 `=>`。它不是「現代 C# 的勳章」，能不能轉成 block 讓可讀性變好，才是取捨依據。**簡潔有價值，但可讀性的價值更高。**

---

### 2️⃣ `const` 與 `readonly`：解決的是不同的問題

兩者乍看都是「讓值不可變」，但機制完全不同。

| | `const` | `readonly` |
|---|---|---|
| 決定時機 | **編譯期常數** | **執行期**才賦值/讀取 |
| 編譯後行為 | 值被**直接內嵌（inline）**進呼叫端的組件（assembly） | 透過 `ldsfld` 在執行期讀取欄位值 |
| 能否指派非常數運算式 | 不行（例如 `DateTime.UtcNow` 不能是 `const`） | 可以（`static readonly DateTime Started = DateTime.UtcNow;`） |
| 修改後，消費端何時看到新值 | **必須重新編譯**才能看到新值 | **不需要重新編譯**，執行期直接讀到新值 |

最關鍵的地雷在這裡：

```csharp
// Library 內
public const int DefaultTimeout = 30;
```

應用程式參考這個套件並編譯後，`30` 這個字面值已經被**燒錄（bake）進消費端的組件**裡了。之後 Library 把常數改成 `60`：

```csharp
public const int DefaultTimeout = 60;
```

**消費端不會看到新值，除非重新編譯。** 這不是 bug，是 `const` 的預期行為，但很多人沒意識到這一點。

`readonly` 沒有這個問題，因為值是執行期才解析：

```csharp
public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
```

**經驗法則**：`const` 只留給「真正全宇宙通用、永遠不會變」的值（數學常數、固定協定值）；其他情況一律用 `readonly`。多數資深工程師的預設值是 `readonly`，除非有明確理由才用 `const`。

> 💡 **與實務的連結**：這個問題在你維護「供其他 Repo 引用的 NuGet 套件」（例如作為 client SDK 的專案）時特別致命——如果套件對外暴露 `public const`，之後改值，所有還沒重新編譯、重新安裝套件的消費端專案都會停留在舊值，而且完全不會出現任何警告或錯誤，只是行為悄悄跟你預期的不一樣。這正是為什麼 .NET Runtime 官方的 API 設計指南建議：**公開給外部消費的常數，優先用 `static readonly`，而不是 `const`。**

---

### 3️⃣ 二進位字面值與數字分隔符：不是為了少打字，是為了讓意圖一眼可見

```csharp
const int Read    = 0b0001;
const int Write   = 0b0010;
const int Execute = 0b0100;
const int Delete  = 0b1000;
```

直接看二進位表示的位元，遠比在腦中把十進位轉成二進位更直覺。數字分隔符 `_` 也是同樣的道理：

```csharp
const int MaxUsers = 1_000_000;
const long FileSize = 10_737_418_240;

// 對比看看：
const int MaxUsers = 1000000;
const long FileSize = 10737418240;
```

兩者的值完全相同，差別只在於後者逼著讀者自己數位數。分隔符對十六進位、二進位一樣有效：

```csharp
const int Color = 0xFF_FF_00;
const int Flags = 0b1010_1100_0001_1110;
```

**分隔符對執行期零成本**——編譯器會完全忽略它們，純粹是給人看的。這個功能不是要你寫出很潮的程式碼，而是**降低讀者理解既有數值所需的心力**。

---

### 4️⃣ `Span<T>` 快，是因為編譯器不信任你

`Span<T>` 是現代 C# 最重要的效能功能之一：能操作記憶體切片，不需要額外配置陣列或複製資料。

```csharp
Span<int> numbers = stackalloc[] { 1, 2, 3, 4, 5 };
Span<int> firstThree = numbers[..3];
```

兩個 span 指向同一塊底層記憶體，效率極高。但這個安全性伴隨著嚴格的生命週期規則。以下這些寫法**都無法通過編譯**：

```csharp
// ❌ 不能從方法回傳 stackalloc 的 Span<T>
public Span<int> GetNumbers()
{
    Span<int> values = stackalloc[] { 1, 2, 3 };
    return values; // 方法返回後，stack frame 的記憶體就消失了
}

// ❌ 不能當作 class 的欄位
public class BufferHolder
{
    private Span<byte> _buffer; // 編譯失敗
}

// ❌ 不能被 lambda 捕捉（closure）
Span<int> values = stackalloc[] { 1, 2, 3 };
Action action = () =>
{
    Console.WriteLine(values[0]); // 編譯失敗
};
```

**原因**：`stackalloc` 配置的記憶體活在目前的 stack frame 上，方法一返回，記憶體就消失了。如果 `Span<T>` 能被回傳、存成欄位、或被 lambda 捕捉，呼叫端就可能拿到一個指向無效記憶體的參照。

這些限制不是編譯器隨便設下的規則，而是**讓 `Span<T>` 同時具備安全性與零配置（allocation-free）的關鍵**：編譯器不是靠執行期檢查或 GC 來防止非法記憶體存取，而是在**編譯期就證明** span 不可能活得比它參照的記憶體久。抓住這個原則之後，`Span<T>` 大部分的規則就不再顯得莫名其妙——它們全部在強制同一個保證：**span 永遠不能活得比它底層的儲存空間久。**

---

### 5️⃣ `default` 與 `new()`：長得一樣，說的故事不同

對 value type 而言，這兩行的結果相同：

```csharp
Point p1 = default;
Point p2 = new();
```

兩個變數都是零初始化。既然結果一樣，為什麼要有兩種寫法？**因為它們表達的意圖不同。**

- 寫 `default`，你在說：「給我這個型別的預設值」
- 寫 `new()`，你在說：「建立一個新的實例」

這個差異在泛型程式碼中變得更重要：

```csharp
public T Create<T>() where T : new()
{
    return new(); // 依賴泛型限制式（constraint），必須有 public 無參數建構子
}

public T GetDefault<T>()
{
    return default!; // 不需要建構子限制式，因為每個型別都有預設值
}
```

執行期行為經常相同，但**語意不同**：一個表達「預設初始化」，一個表達「建構」。在泛型 API 中，選對符合意圖的寫法，能讓程式碼更容易理解。

---

## 📊 五個功能快速對照表

| 功能 | 表面上看起來 | 實際上在意的是 |
|------|--------------|----------------|
| Expression-Bodied Members | 讓程式碼更簡潔 | 可讀性優先於語法簡潔，別為了單行而單行 |
| `const` vs `readonly` | 都是不可變 | `const` 編譯期內嵌（有跨組件版本風險）；`readonly` 執行期讀取 |
| 二進位字面值／數字分隔符 | 少打幾個字 | 讓數值意圖一眼可見，零執行期成本 |
| `Span<T>` 的限制 | 编譯器很囉嗦 | 編譯期證明生命週期安全，換取零配置的高效能 |
| `default` vs `new()` | 結果一樣 | 語意不同（預設值 vs 建構），泛型中差異更明顯 |

---

## 🎯 資深工程師評論

### 整體評價

這篇文章選題精準——它挑的五個功能全部符合「你每天在用，但可能從沒細想過」的標準，而且巧妙地避開了兩種常見的爛文章套路：既不是把新手教學重講一次，也不是堆砌冷門語法炫技。每一節都有清楚的「表面現象 → 底層機制 → 實務建議」三段式結構，這是讓一篇技術文章真正有價值的關鍵——**它解釋的是「為什麼」，而不只是「規則是什麼」**。

---

### 值得肯定的地方

**`const` 那一段是全文含金量最高的部分。** 很多 C# 工程師寫了好幾年都沒意識到 `const` 會被「烘焙」進消費端組件這件事，直到真的遇到「明明改了設定值，Deploy 了卻沒生效」的詭異案例，才發現問題出在 NuGet 套件版本沒更新、消費端沒重新編譯。這是一個**只有在維護共用套件／SDK 時才會現形的地雷**，文章能把這個機制講清楚，價值遠高於單純告訴讀者「兩個都能做到不可變」。

**`Span<T>` 那一段把「規則」提煉成「原則」，這是資深工程師與一般工程師的分水嶺。** 很多人記住了「`Span<T>` 不能存成欄位、不能被 lambda 捕捉」這些規則，卻說不出為什麼。文章給出的「span 不能活得比它的底層儲存空間久」這句話，是理解一整組限制的萬用鑰匙——這種「用一個原則統一多條規則」的講法，比死背規則清單有效率得多。

**Expression-bodied members 那節提醒了一個常被忽略的事實：語法糖本身不是目標，可讀性才是。** 很多 Code Review 上的意見分歧（該不該用 `=>`），其實可以用「這個成員未來會不會長大」這個問題來簡化判斷——如果答案是「可能會」，就不要一開始硬塞進單行。

---

### 可以更深入的地方（本文額外補充）

**1. `Span<T>` 的所有限制，其實都來自同一個根本原因：它是 `ref struct`。**

文章把每個限制個別列出來（不能回傳、不能當欄位、不能被 lambda 捕捉），但沒有點出這些規則背後共同的語言機制——`Span<T>` 被宣告為 `ref struct`，而 `ref struct` 在 C# 中有一組專屬限制：

- **只能存在於 stack 上**，不能被 boxing，不能是一般 class 的欄位（因為 class 實例活在 heap 上）
- **不能實作 interface**（因為透過 interface 呼叫可能導致 boxing）
- **不能被 lambda 或區域函式捕捉**（因為 closure 在背後會被編譯成一個 heap 上的 class）
- **不能跨越 `await` 邊界**（因為 async 方法的區域變數可能被搬進 heap 上的 state machine）

理解「因為它是 `ref struct`」這個根本原因，比記住四五條個別規則更容易類推到新情境——例如你會立刻知道，同樣身為 `ref struct` 的 `ReadOnlySpan<T>`、`SpanAction<T>`，甚至你自己定義的 `ref struct`，都會有一模一樣的限制。

**2. `default` 與 `new()` 在 C# 10 之後，對於自訂建構子的 struct 其實可能產生不同結果——這是文章沒提到、卻很容易在實務中踩到的坑。**

文章的例子隱含假設 `Point` 是一個「所有欄位都用預設值」的傳統 struct，這種情況下 `default` 和 `new()` 確實等價。但 C# 10 開始，struct 可以自訂無參數建構子：

```csharp
public struct Point
{
    public int X;
    public int Y;

    // C# 10 開始合法：struct 的無參數建構子
    public Point()
    {
        X = -1;
        Y = -1;
    }
}

Point p1 = default;  // X=0, Y=0 —— 永遠繞過自訂建構子，直接零初始化記憶體
Point p2 = new();    // X=-1, Y=-1 —— 會呼叫你自訂的無參數建構子
```

**`default` 永遠不會呼叫使用者自訂的無參數建構子**，它就是單純把記憶體清零；`new()` 則一定會呼叫建構子（如果有自訂的話）。這代表一旦 struct 定義了自訂無參數建構子，文章說的「兩者結果相同」就不再成立。這個細節在泛型程式碼（`where T : new()`）中特別危險：如果你誤以為 `default(T)` 可以安全地替代 `new T()` 來取得一個「已正確初始化」的實例，在這種情境下會得到錯誤的結果。

**3. `const` vs `readonly` 的差異，其實也能從 IL（中繼語言）層級驗證，而不只是行為描述。**

`const` 欄位在 IL 中是 `literal` field，沒有實體記憶體位置，使用時編譯器直接用 `ldc.i4`／`ldstr` 等指令內嵌字面值；`readonly` 欄位是 `initonly` field，有實體記憶體位置，使用時透過 `ldsfld` 在執行期讀取。想親自驗證這個差異的話，可以用 `ildasm` 或 [SharpLab](https://sharplab.io/) 對照兩種寫法編譯後的 IL，眼見為憑會比單純接受行為描述更有說服力。

**4. 這篇文章談的都是「不可變性」與「初始化」的其中一部分，還有幾個緊密相關但沒提到的功能值得一併認識：**

| 功能 | 與本文的關聯 | 一句話 |
|------|--------------|--------|
| `readonly struct` | 呼應第 2 點 | 整個 struct 都不可變，編譯器可以放心用 `in` 傳遞而不需要防禦性複製 |
| `in` 參數 | 呼應第 2、4 點 | 用參照傳遞唯讀大型 struct，避免複製成本，效果類似「唯讀版的 `ref`」 |
| `init` 存取子 | 呼應第 2 點 | 物件建構完成後就不可變，但比 `readonly` 欄位更彈性（支援物件初始設定式） |
| `ref readonly` 回傳 | 呼應第 4 點 | 回傳大型 struct 的參照且禁止呼叫端修改，常與 `Span<T>` 搭配優化熱路徑 |

---

### 給日常 .NET 開發的實務建議

1. **Code Review 時看到 `const` 用在 public API／共用套件上，多問一句「這個值未來有可能會變嗎？」**，如果答案是「有可能」，直接建議改成 `static readonly`，避免消費端日後遇到「改了設定沒生效」的詭異狀況。
2. **看到多層巢狀三元運算子塞進 `=>` 成員時，這是很好的 Code Review 意見切入點**——不是語法錯誤，但值得討論「這樣寫真的比較好懂嗎？」
3. **如果你的程式碼會操作大量 buffer（例如解析檔案、處理網路封包），`Span<T>`／`ReadOnlySpan<T>` 是值得投資的效能工具**，但要有心理準備：它的限制不是 bug，是特性，理解 `ref struct` 的本質能讓你少走很多冤枉路。
4. **在寫泛型工廠方法時，`default` 跟 `new()` 不要混用當作「反正結果一樣」**，尤其未來可能有人把型別參數換成一個帶自訂建構子的 struct，屆時行為會悄悄改變且不會有編譯錯誤提醒你。

---

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐⭐☆ |
| 新穎性 | ⭐⭐⭐☆☆（皆為既有語言功能，但角度選得好） |
| 適合對象 | 有 1-3 年 C# 經驗、開始寫 Public API／關注效能的工程師 |

**這是一篇「打磨細節」等級的文章**，不會讓你的架構能力一夜提升，但這五個功能背後的取捨，正是資深與資淺 C# 工程師之間最容易被面試官或 Code Review 抓出來的分水嶺。如果想更系統性地延伸閱讀：

- 官方文件 [Write safe and efficient C# code](https://learn.microsoft.com/dotnet/csharp/write-safe-efficient-code) — `Span<T>`、`ref struct`、`stackalloc` 的完整規則
- 官方文件 [Constants and readonly fields](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/constants) — `const`/`readonly` 的官方權威說明
- [.NET Runtime API 設計指南](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/api-guidelines/nullability.md) 系列文件 — 了解為什麼公開 API 偏好 `readonly` 而非 `const`
