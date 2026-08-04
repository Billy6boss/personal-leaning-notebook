# 《Factory Pattern（下）：一步一步實作一個簡單 DI Container》繁體中文總結與評論

> 原文：Factory Pattern (Part 2): Step-by-Step Guide to Implementing a Simple DI Container
> 原文背景：Java / Spring IoC 容器概念
> 筆記補充：**本文所有程式碼範例改寫為 C# / .NET，並對照 Microsoft.Extensions.DependencyInjection 的原生做法**

---

## 📋 文章總結

這篇文章的核心觀點是：**DI Container 本質上是 Factory Pattern 的超級升級版，它不只負責建立物件，還負責理解依賴、決定建立時機、管理整個生命週期。** 原文用 Spring XML + Java Reflection 手把手拆解一個簡化版容器，這對 .NET 工程師特別值得看，因為你會發現 `IServiceCollection`、`ServiceProvider`、`AddSingleton` / `AddScoped` / `AddTransient` 背後，其實也是同一套「註冊資訊 + 反射建構 + 生命週期管理」的基本思想。

---

### 1. DI Container 與 Factory Pattern 的關係：不是取代，而是放大

白話來說，普通 Factory 通常只負責某一小群物件，例如 `ParserFactory` 專門建立 parser；DI Container 則像「全系統物件總管家」，它要處理整個應用程式的物件建立、相依性串接與重用策略。

**核心思想：DI Container 不是脫離 Factory Pattern，而是把 Factory 從「單點物件建立」升級成「全域物件管理系統」。**

#### ❌ 沒有容器時：業務邏輯自己 new 相依物件

```csharp
public class UserService
{
    private readonly DbConnection _dbConnection;

    public UserService()
    {
        _dbConnection = new DbConnection(
            "Server=127.0.0.1;Database=AppDb;",
            "sa",
            "123456");
    }

    public void QueryUser()
    {
        Console.WriteLine($"Query user | db={_dbConnection.ConnectionString}");
    }
}
```

#### ✅ 使用 .NET 內建 DI：建立與使用分離

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton(new DbConnection(
    "Server=127.0.0.1;Database=AppDb;",
    "sa",
    "123456"));
services.AddTransient<UserService>();

using var serviceProvider = services.BuildServiceProvider();
var userService = serviceProvider.GetService<UserService>();
userService?.QueryUser();

public class UserService
{
    private readonly DbConnection _dbConnection;

    public UserService(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public void QueryUser()
    {
        Console.WriteLine($"Query user | db={_dbConnection.ConnectionString}");
    }
}

public class DbConnection
{
    public DbConnection(string connectionString, string username, string password)
    {
        ConnectionString = connectionString;
        Username = username;
        Password = password;
    }

    public string ConnectionString { get; }
    public string Username { get; }
    public string Password { get; }
}
```

在 Spring 世界裡，原文用 XML 告訴容器要建立哪些 bean；在 .NET 世界裡，這件事通常直接寫在 `Program.cs` / `Startup.cs`：

```csharp
builder.Services.AddSingleton<IDbConnection, SqlDbConnection>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddTransient<UserService>();
```

這是兩種不同的「註冊語法」，但本質相同：**都在把「如何建立物件」從業務程式碼抽離出來。**

---

### 2. 第一件核心工作：讀取設定，讓容器知道要建立什麼

原文先強調，DI Container 不可能把你系統裡所有 class 都硬編碼進框架，所以一定要有一個地方描述：有哪些物件、它們的型別、依賴誰、生命週期是什麼。

**核心思想：容器的第一步不是建立物件，而是先擁有一份「建立藍圖」。**

Spring 的做法是 XML：

```xml
<bean id="dbConnection" class="com.example.DbConnection">
    <constructor-arg type="String" value="jdbc:mysql://127.0.0.1:3306/test" />
    <constructor-arg type="String" value="root" />
    <constructor-arg type="String" value="123456" />
</bean>

<bean id="userService" class="com.example.UserService">
    <constructor-arg ref="dbConnection" />
</bean>
```

.NET 內建 DI 不走 XML，而是走程式碼註冊：

#### ❌ 把依賴關係散落在各處

```csharp
var dbConnection = new DbConnection(connectionString, username, password);
var userRepository = new UserRepository(dbConnection);
var userService = new UserService(userRepository);
```

#### ✅ 在組態根集中註冊依賴關係

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton(new DbConnection(connectionString, username, password));
services.AddScoped<UserRepository>();
services.AddScoped<UserService>();

using var serviceProvider = services.BuildServiceProvider();
var userService = serviceProvider.GetService<UserService>();
```

如果你真的想走外部設定驅動，.NET 生態系通常會用：

- `Microsoft.Extensions.Configuration` + 程式碼註冊
- Autofac 的 Module / Assembly Scanning
- Unity、Autofac 等第三方容器的 XML / JSON / Module 擴充能力

也就是說，**.NET 不是不能做設定式 DI，而是主流做法更偏向「C# Code as Configuration」。**

---

### 3. 第二件核心工作：用 Reflection 動態建立物件，而不是寫死 new

原文的關鍵轉折在這裡：如果容器要管理整個系統的物件，它不可能為每個 class 各寫一個工廠，所以只能仰賴 Reflection，在執行期動態找到型別、選擇建構子、建立實例。

**核心思想：Reflection 讓容器從「只能建立我事先知道的型別」進化成「只要有註冊資訊就能動態建立任何型別」。**

#### ❌ 寫死的工廠很快會失控

```csharp
public sealed class ObjectFactory
{
    public object Create(string name)
    {
        return name switch
        {
            "dbConnection" => new DbConnection("Server=127.0.0.1;Database=AppDb;", "sa", "123456"),
            "userService" => new UserService(
                new DbConnection("Server=127.0.0.1;Database=AppDb;", "sa", "123456")),
            _ => throw new InvalidOperationException($"Unknown service | name={name}")
        };
    }
}
```

#### ✅ 以 .NET Reflection / Activator 動態建立

```csharp
using System.Reflection;

public static object CreateInstance(Type implementationType, object[] arguments)
{
    return Activator.CreateInstance(implementationType, arguments)
        ?? throw new InvalidOperationException(
            $"Unable to create instance | type={implementationType.FullName}");
}

var type = Type.GetType("MyApp.Services.UserService, MyApp")
    ?? throw new InvalidOperationException("Type not found | name=MyApp.Services.UserService, MyApp");
```

不過在 .NET 實務裡，更貼近內建 DI 思維的 API 是：

```csharp
using Microsoft.Extensions.DependencyInjection;

var service = ActivatorUtilities.CreateInstance<UserService>(serviceProvider);
```

這個 API 的價值是：**你不需要手動把所有建構子參數都湊齊，容器會從 `IServiceProvider` 幫你補依賴。** 這也剛好呼應很多人容易忽略的事：`ActivatorUtilities` 本身就是 .NET 官方提供的一個「半手動、半容器化」建構工具。

---

### 4. 第三件核心工作：管理物件生命週期，而不只是建出來就算了

原文把 singleton / prototype、lazy-init、init / destroy method 都納入容器責任，這是非常關鍵的觀念。真正的容器不是 `new` 的集中器，而是**生命週期的協調者**。

**核心思想：建立物件只是開始，決定「何時建立、是否重用、何時釋放」才是容器真正有價值的地方。**

#### Spring 與 .NET 生命週期對照

| 原文概念 | Spring | .NET 對照 | 補充 |
|------|------|------|------|
| 單例 | `scope="singleton"` | `ServiceLifetime.Singleton` / `AddSingleton()` | 整個容器共用同一份實例 |
| 原型 | `scope="prototype"` | `ServiceLifetime.Transient` / `AddTransient()` | 每次解析都建立新物件 |
| 請求範圍 | request/session scope | `ServiceLifetime.Scoped` / `AddScoped()` | .NET 內建 DI 很常用，ASP.NET Core 每個 request 一個 scope |

這裡有一個很值得記的差異：**`.NET` 的 `Scoped` 是一等公民，但原文討論的 Spring 簡化版本沒有直接對應到這個概念。** 若放到 Web 應用情境，Spring 常談 request scope / session scope；ASP.NET Core 則把它抽象成通用的 `IServiceScope`。

#### ❌ 每次都自己 new，完全沒生命週期策略

```csharp
public UserService CreateUserService()
{
    var dbConnection = new DbConnection(connectionString, username, password);
    var repository = new UserRepository(dbConnection);
    return new UserService(repository);
}
```

#### ✅ 交由容器決定重用策略

```csharp
builder.Services.AddSingleton<IDbConnection, SqlDbConnection>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddTransient<UserService>();
```

另外，.NET 內建 DI 雖然不像舊式 Spring XML 常直接寫 `init-method` / `destroy-method`，但它仍然有生命週期管理能力：

- `IDisposable` / `IAsyncDisposable`：由容器在 scope 或 root provider 結束時釋放
- `IHostedService`：可視為應用程式啟動 / 結束的生命週期掛點
- Lazy 建立：多數 service 本來就是「第一次被 resolve 才建立」，除非你自己在 startup 主動解析

---

### 5. 原文手把手實作的骨架：ApplicationContext、Parser、BeanDefinition、BeansFactory

原文的教學價值很高，因為它把容器拆成幾個乾淨的角色，而不是一坨大 class：

- `ApplicationContext`：對外入口，只暴露 `getBean()`
- `ClassPathXmlApplicationContext`：負責讀設定、初始化容器
- `BeanConfigParser` / `XmlBeanConfigParser`：把 XML 轉成統一資料結構
- `BeanDefinition`：描述每個 bean 的型別、建構子參數、scope、lazy-init
- `BeansFactory`：真正負責建立與快取物件

**核心思想：一個容器的關鍵不是「會 new」，而是把註冊資訊、解析流程、建立流程、快取策略拆成不同責任。**

在 .NET 語言下，可以把這組角色概念性對照成：

#### Java / Spring 風格概念

```text
ApplicationContext -> 對外拿物件的入口
BeanDefinition -> 每個服務的註冊資訊
BeansFactory -> 根據定義建立物件
```

#### .NET 對照觀念

```csharp
IServiceCollection services = new ServiceCollection(); // 註冊資訊集合
IServiceProvider serviceProvider = services.BuildServiceProvider(); // 建立後的解析器
var userService = serviceProvider.GetService<UserService>(); // 對外入口
```

雖然 `IServiceCollection` / `ServiceProvider` 不完全等於原文各個 class，但工程上的角色幾乎一樣：**先收集定義，再建立 provider，最後由 provider 解析物件。**

---

### 6. 簡化版 C# DI Container：用最少程式碼看懂底層概念

如果只想抓住原理，不追求 production-ready，我認為下面這個 C# 版本最適合拿來對照原文。它保留了三件核心事：註冊、反射建構、singleton cache。

**核心思想：自己寫一次簡化版容器後，你會更清楚官方 DI 容器解決了哪些麻煩。**

#### ✅ 簡化版 C# DI Container 範例

```csharp
using System.Reflection;

public sealed class SimpleContainer
{
    private readonly Dictionary<string, ServiceDefinition> _definitions = new();
    private readonly Dictionary<string, object> _singletonCache = new();

    public void Register(ServiceDefinition definition)
    {
        _definitions[definition.Id] = definition;
    }

    public object GetService(string id)
    {
        if (!_definitions.TryGetValue(id, out var definition))
            throw new InvalidOperationException($"Service not registered | id={id}");

        if (definition.Lifetime == ServiceLifetime.Singleton &&
            _singletonCache.TryGetValue(id, out var existing))
        {
            return existing;
        }

        var instance = CreateInstance(definition);

        if (definition.Lifetime == ServiceLifetime.Singleton)
            _singletonCache[id] = instance;

        return instance;
    }

    private object CreateInstance(ServiceDefinition definition)
    {
        var implementationType = definition.ImplementationType;
        var constructor = implementationType
            .GetConstructors()
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No public constructor found | type={implementationType.FullName}");

        var arguments = constructor.GetParameters()
            .Select(parameter =>
            {
                var dependency = _definitions.Values.FirstOrDefault(x => x.ServiceType == parameter.ParameterType)
                    ?? throw new InvalidOperationException(
                        $"Dependency not registered | serviceType={parameter.ParameterType.FullName}");

                return GetService(dependency.Id);
            })
            .ToArray();

        return Activator.CreateInstance(implementationType, arguments)
            ?? throw new InvalidOperationException(
                $"Failed to create instance | type={implementationType.FullName}");
    }
}

public sealed class ServiceDefinition
{
    public required string Id { get; init; }
    public required Type ServiceType { get; init; }
    public required Type ImplementationType { get; init; }
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Transient;
}

public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
}
```

使用方式：

```csharp
var container = new SimpleContainer();

container.Register(new ServiceDefinition
{
    Id = "dbConnection",
    ServiceType = typeof(DbConnection),
    ImplementationType = typeof(DbConnection),
    Lifetime = ServiceLifetime.Singleton
});

container.Register(new ServiceDefinition
{
    Id = "userService",
    ServiceType = typeof(UserService),
    ImplementationType = typeof(UserService),
    Lifetime = ServiceLifetime.Transient
});

var userService = (UserService)container.GetService("userService");
```

這個版本故意沒有做太多功能，所以你更容易看到原文的本質：

1. 先有一份註冊資訊
2. 解析時先找建構子
3. 遞迴建立依賴
4. 視生命週期決定是否快取

這就是最小可理解版的 DI Container。

---

### 7. 用 .NET 原生 DI 回頭看原文：你平常其實都在用同一套概念

很多人學 DI 時，只記住 `AddScoped()`、`AddSingleton()` 怎麼背，卻沒有真的把它和底層原理連起來。原文最大的價值就在這裡：它把容器「去魔法化」了。

**核心思想：一旦理解容器底層只是註冊表 + 建構策略 + 快取策略，你就比較不會把框架 API 當黑盒。**

#### ✅ 以 .NET 內建 DI 重新表達原文的使用流程

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<DbConnection>(_ =>
    new DbConnection("Server=127.0.0.1;Database=AppDb;", "sa", "123456"));
services.AddTransient<UserService>();

using var serviceProvider = services.BuildServiceProvider();

var userService = serviceProvider.GetService<UserService>()
    ?? throw new InvalidOperationException("UserService not found");

userService.QueryUser();
```

如果你再往下想一層：

- `AddSingleton()` = 把定義放進註冊表，並標記 singleton
- `BuildServiceProvider()` = 準備好解析規則與快取機制
- `GetService<T>()` = 依型別找到定義，遞迴建立依賴，必要時從 cache 取回

你就會理解：**容器 API 只是比較成熟、比較安全、比較完整的 `BeansFactory` 介面。**

---

## 🎯 資深工程師評論

### 整體評價

這篇文章非常適合拿來當作「從 Factory Pattern 走向 DI / IoC 理解」的橋樑。它沒有一開始就把 Spring 容器講成一堆名詞，而是先把問題定義清楚：當物件建立開始涉及設定、相依性和生命週期時，單純的工廠模式就不夠了，於是 DI Container 成為一個自然演化的結果。對已經在 .NET 實務裡每天寫 `builder.Services` 的工程師來說，這種拆解方式很有價值，因為它會把原本熟悉但抽象的 API 還原成可推理的機制。

---

### 值得肯定的地方

**第一，文章把 DI Container 的職責濃縮成三件事：讀設定、建物件、管生命週期。** 這個切法非常好，因為它比背 IoC / DI / BeanFactory / ApplicationContext 等術語更容易建立穩固的心智模型。很多人對容器的理解停在「會自動注入」，但真正重要的是：它先有定義，再有建立，再有重用與釋放。

**第二，原文選擇從手刻 `BeanDefinition` 與 `BeansFactory` 進入，而不是直接講框架 API。** 這對資深工程師特別重要，因為只要你看過這種最小實作，就會知道所有成熟 DI 容器的複雜度主要都加在哪裡：例外處理、執行緒安全、循環依賴偵測、scope 管理、open generic、factory delegate、disposal、效能最佳化。

**第三，這篇文章很適合和 .NET 內建 DI 一起對照閱讀。** Spring XML 雖然不是 .NET 主流，但它把「註冊資訊外部化」這件事展示得很直觀。回頭看 ASP.NET Core 的 `Program.cs`，你會更清楚那其實只是把 XML `<bean>` 改成 C# `AddSingleton()` / `AddScoped()` / `AddTransient()`。

---

### 可以更深入的地方

**第一個缺口是循環依賴（Circular Dependency）偵測。** 原文的簡化版容器用遞迴方式建立建構子依賴，這在教學上很好懂，但一旦 A 依賴 B、B 又依賴 A，就會無限遞迴或在某個階段失敗。正式的 DI 容器（包含 .NET 內建容器）都會在解析鏈中偵測循環依賴，並拋出清楚的例外訊息。這是所有「自己手寫簡易容器」最常踩到的坑之一，也正是 production-grade 容器存在的理由。

**第二個缺口是 scope 模型沒有延伸到 Web 實務。** 原文只談 singleton / prototype，這是理解容器的好起點，但若讀者主要背景是 ASP.NET Core，就一定要補上 `Scoped`。在 Web 應用裡，`DbContext`、request-level cache、tenant context 幾乎都依賴 scope；如果只理解 singleton / transient，很容易在實務上做出錯誤生命週期配置。

**第三個可以補充的是反射 API 的現代用法。** 原文在 Java 端提到無參數建構子的 `newInstance()` 風格，但現代 Java 已經不建議直接使用 `Class.newInstance()`，而是建議改用 `Constructor.newInstance()`，因為例外處理與存取控制比較清楚。這雖然是小細節，但對技術筆記來說值得順手點出，避免讀者把教學範例直接帶進正式程式。

**第四個可延伸主題是效能最佳化。** 真正成熟的 DI 容器通常不會每次都靠慢速 reflection 現場硬解；很多會把建構流程編譯成 expression tree、delegate，或在啟動時預建解析計畫。原文刻意不談這塊是合理的，但若要再往下一層理解框架設計，這會是很好的延伸方向。

---

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐⭐☆ |
| 新穎性 | ⭐⭐⭐☆☆ |
| 適合對象 | 中階到資深後端工程師、正在補 DI / IoC 底層概念的 .NET 開發者 |

**如果你已經會用 DI，但一直把容器當黑盒，這篇文章很值得讀；它不是教你更多 API，而是幫你建立比較穩的底層模型。**

### 延伸閱讀

- [Microsoft Docs - Dependency injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Microsoft Docs - IServiceCollection Interface](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)
- [Microsoft Docs - ActivatorUtilities Class](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.activatorutilities)
- [Spring Framework Reference - Core Technologies, The IoC Container](https://docs.spring.io/spring-framework/reference/core/beans/introduction.html)
- *Dependency Injection Principles, Practices, and Patterns* — Mark Seemann, Steven van Deursen
- *CLR via C#* — Jeffrey Richter
