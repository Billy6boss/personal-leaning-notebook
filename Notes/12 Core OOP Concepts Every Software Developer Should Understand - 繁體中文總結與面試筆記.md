# 《每個軟體工程師都該懂的 12 個核心 OOP 概念》繁體中文總結與面試筆記

> 原文：12 Core OOP Concepts Every Software Developer Should Understand
> 筆記用途：**面試複習**（OOP 基礎 / Low-Level Design 常考題）
> 原文範例語言：Java（本筆記額外補充 C# 對照，因應面試情境更貼近實務背景）

---

## 📋 文章總結

這篇文章把 OOP 拆成三大層次、共 12 個核心概念：

1. **核心建構元素**：Class、Object、Interface — OOP 的積木
2. **四大支柱**：Encapsulation、Abstraction、Inheritance、Polymorphism — 讓 OOP 強大的設計原則
3. **物件關係**：Association、Aggregation、Composition、Dependency、Realization — 物件之間如何連結

> ⚠️ **原文小提醒**：原文標題說是「12 個概念」，但實際上 Aggregation 段落結尾明確預告「接下來要講 Composition」，正文卻直接跳到 Dependency，**Composition 沒有獨立段落與程式碼範例**（這是原文的疏漏）。由於 Composition 是面試「必考」題（Aggregation vs Composition、Composition over Inheritance），本筆記在下方 **物件關係** 章節中已補齊完整說明，確保面試準備不會有缺口。

核心心法只有一句話：**OOP 不是為了寫 class，而是為了把「真實世界的東西」用結構化、可維護、可擴充的方式表達出來。**

---

## 🧱 Part 1：核心建構元素（Core Building Blocks）

### 1️⃣ Class（類別）

**白話解釋**：Class 是藍圖／模板，定義「有什麼資料」和「能做什麼行為」，但它本身不是一個實際可用的東西（就像房屋設計圖不能住人）。

```java
public class Car {
    private String brand;
    private String color;
    private int speed;

    public Car(String brand, String color) {
        this.brand = brand;
        this.color = color;
        this.speed = 0;
    }

    public void increaseSpeed(int value) {
        speed += value;
    }
}
```

**面試重點**：Class 只是型別定義，不佔用「物件」的記憶體語意；重點是把相關的資料與行為封在一起，取代散落各處的變數 + 函式。

---

### 2️⃣ Object（物件）

**白話解釋**：Object 是根據 Class 這張藍圖「實際建出來」的東西。同一個 Class 可以產生多個 Object，每個 Object 有自己獨立的資料狀態。

```java
Car car1 = new Car("Toyota", "Red");
Car car2 = new Car("Honda", "Blue");
car1.increaseSpeed(40); // 不影響 car2
```

**面試重點**：Class 給結構，Object 給實際的值；物件之間彼此獨立，這是「狀態封裝在實例上」的基礎，也是後面 Encapsulation 的前提。

---

### 3️⃣ Interface（介面）

**白話解釋**：Interface 是一份「契約」，只規定「必須有哪些方法」，不規定「怎麼實作」。

```java
public interface NotificationService {
    void sendMessage(String user, String message);
}

public class EmailNotification implements NotificationService {
    public void sendMessage(String user, String message) {
        System.out.println("Email to " + user + ": " + message);
    }
}
```

呼叫端只依賴介面，不依賴實作：

```java
public class AlertManager {
    private final NotificationService notificationService;

    public AlertManager(NotificationService notificationService) {
        this.notificationService = notificationService;
    }

    public void sendAlert(String user) {
        notificationService.sendMessage(user, "Your order has been shipped.");
    }
}
```

明天要加 `PushNotification`，完全不用改 `AlertManager`——**這正是 SOLID 中的開放封閉原則（OCP）與依賴反轉原則（DIP）在實務上的體現**（詳見下方 SOLID 加碼章節）。

**面試重點**：Interface 定義「What」，Class 決定「How」；這是達成多型（Polymorphism）與依賴注入（DI）的基礎。

---

## 🏛️ Part 2：OOP 四大支柱（The 4 Pillars）

### 4️⃣ Encapsulation（封裝）

**白話解釋**：把資料設為 `private`，只透過方法（`public` 的 getter/setter 或業務方法）存取，讓 Class 自己控制資料何時、如何被改變，避免物件進入不合法的狀態。

```java
public class BankAccount {
    private double balance; // 外部無法直接改

    public void deposit(double amount) {
        if (amount > 0) balance += amount;
    }

    public void withdraw(double amount) {
        if (amount > 0 && amount <= balance) balance -= amount;
    }

    public double getBalance() { return balance; }
}
```

如果 `balance` 是 `public`，任何人都能寫 `account.balance = -10000;`，帳戶邏輯完全失去保護。

**面試重點**：Encapsulation 保護的是「資料」——**How to access（怎麼存取）**。

---

### 5️⃣ Abstraction（抽象化）

**白話解釋**：隱藏複雜的實作細節，只暴露簡單、必要的操作介面。像 ATM：使用者只要插卡、輸入密碼、領錢，不需要知道背後怎麼跟銀行主機溝通。

```java
public class FoodOrder {
    public void placeOrder(String foodItem, String location) {
        checkRestaurantAvailability(foodItem);
        calculateDeliveryCharge(location);
        applyDiscount();
        assignDeliveryPartner();
        sendConfirmation();
    }
    // 以下皆為 private，外部看不到、也不需要知道
    private void checkRestaurantAvailability(String foodItem) { /* ... */ }
    private void calculateDeliveryCharge(String location) { /* ... */ }
    private void applyDiscount() { /* ... */ }
    private void assignDeliveryPartner() { /* ... */ }
    private void sendConfirmation() { /* ... */ }
}
```

**面試重點**：Abstraction 隱藏的是「流程／複雜度」——**What to expose（暴露什麼）**。這是它跟 Encapsulation 最常被搞混、也最常被拿來考的差異點（詳見下方比較表）。

---

### 6️⃣ Inheritance（繼承）

**白話解釋**：子類別繼承父類別的資料與行為，避免重複程式碼。只在有明確 **is-a** 關係時使用（`Intern` **is a** `Employee`）。

```java
public class Employee {
    protected String name;
    protected String employeeId;
    public void showDetails() { System.out.println(name + " / " + employeeId); }
}

public class FullTimeEmployee extends Employee {
    private double monthlySalary;
    public void calculateSalary() { System.out.println("Salary: " + monthlySalary); }
}

public class Intern extends Employee {
    private double stipend;
    public void calculateStipend() { System.out.println("Stipend: " + stipend); }
}
```

**面試重點**：只因為「想重用程式碼」而繼承，是常見的設計錯誤（fragile base class problem）。判斷準則永遠是「這是不是一個穩固、自然的 is-a 關係？」不是的話，優先考慮 Composition。

---

### 7️⃣ Polymorphism（多型）

**白話解釋**：同一個方法呼叫，因為實際物件不同，而有不同的行為表現。分兩種：

| 類型 | 別名 | 決定時機 | 範例 |
|------|------|----------|------|
| **Compile-time Polymorphism** | Method Overloading（多載） | 編譯期，依「參數列表」決定 | 同名方法，參數型別/數量不同 |
| **Runtime Polymorphism** | Method Overriding（覆寫） | 執行期，依「實際物件型別」動態決定 | 子類別覆寫父類別/介面方法 |

```java
public interface PaymentMethod {
    void pay(double amount);
}

public class CreditCardPayment implements PaymentMethod {
    public void pay(double amount) { System.out.println("Credit Card: " + amount); }
}
public class UpiPayment implements PaymentMethod {
    public void pay(double amount) { System.out.println("UPI: " + amount); }
}

// 呼叫端完全不管實際型別，Java/C# 執行期會自動 dispatch 到正確版本
for (PaymentMethod pm : List.of(new CreditCardPayment(), new UpiPayment())) {
    pm.pay(1000); // 動態決定要跑哪個實作
}
```

**面試重點**：Runtime Polymorphism 才是實務上（尤其是 LLD 設計）真正重要的那種，因為它是「開放封閉原則」的執行機制——新增 `WalletPayment` 完全不用動既有程式碼。

---

## 🔗 Part 3：物件關係（Object Relationships）

四種關係依「耦合強度」由弱到強排列：**Dependency < Association < Aggregation < Composition**。

### 8️⃣ Association（關聯）

**白話解釋**：兩個物件彼此知道對方、互相連結，但生命週期完全獨立，誰也不擁有誰。

```java
public class Teacher {
    private List<Student> students; // 老師「知道」學生，但學生不是老師的一部分
    public void addStudent(Student s) { students.add(s); }
}
```

**例子**：老師教學生、司機開車、顧客下訂單——關聯存在，但沒有「整體與部分」的擁有關係。

---

### 9️⃣ Aggregation（聚合）

**白話解釋**：一種特殊、較弱的「has-a」關係。整體「擁有」部分，但部分**由外部建立、可以獨立存在**，就算整體消失，部分依然存活。

```java
public class School {
    private List<Teacher> teachers;
    public void addTeacher(Teacher t) { teachers.add(t); } // teacher 從外部傳入
}

// Teacher 在 School 之外就已經存在
Teacher t1 = new Teacher("Mr. Sharma", "Math");
School school = new School("Green Valley");
school.addTeacher(t1);

// 同一個 teacher 可以被另一個 school 重複使用 → 生命週期獨立
School anotherSchool = new School("Sunrise Public School");
anotherSchool.addTeacher(t1);
```

**判斷關鍵字**：物件是「從外部傳入」（建構子參數 / setter），學校關閉，老師還在。

---

### 🔟 Composition（組合）— *原文缺漏，此處補齊*

**白話解釋**：比 Aggregation 更強的「has-a」關係。整體「擁有」部分，且**部分的生命週期完全綁定整體**——整體被銷毀，部分也一起消失（"車毀，引擎亡"）。

```java
public class Engine {
    public void start() { System.out.println("Engine started."); }
}

public class Car {
    private final Engine engine; // 在 Car 的建構子「內部」建立

    public Car() {
        this.engine = new Engine(); // 沒有外部注入的入口，Engine 專屬於這台 Car
    }

    public void start() {
        engine.start();
        System.out.println("Car is ready.");
    }
}
```

**例子**：房子與房間、人與心臟、訂單（Order）與訂單明細（OrderItem）——`OrderItem` 沒有 `Order` 就沒有存在意義。

**判斷關鍵字（面試常考的辨別技巧）**：
- 物件是在**建構子內部 `new` 出來**的，還是**從外部傳進來**的？→ 內部 new：Composition；外部傳入：Aggregation
- 刪除/銷毀整體時，部分是否也該一併消失？→ 是：Composition；否：Aggregation

---

### 1️⃣1️⃣ Dependency（依賴）

**白話解釋**：一個 Class **暫時**使用另一個 Class 完成某件事，通常只出現在「方法參數」中，方法執行完，關係就結束了。這是四種關係中**最弱**的一種。

```java
public class FoodOrderService {
    // MessageSender 只在這個方法執行期間被用到，沒有存成欄位
    public void placeOrder(String foodItem, String phone, MessageSender messageSender) {
        System.out.println("Order placed: " + foodItem);
        messageSender.sendMessage(phone, "Order confirmed.");
    }
}
```

**例子**：報表產生器用印表機、結帳服務用折扣計算機、Controller 用驗證服務。

**Association vs Dependency 的關鍵差異**：Association 通常把物件「存成欄位」（長期關係）；Dependency 只在「方法參數」中短暫出現（臨時關係）。

---

### 1️⃣2️⃣ Realization（實現）

**白話解釋**：Class 與它所實作的 Interface 之間的關係——介面訂契約，Class 履行契約、寫出真正的邏輯。

```java
public interface DeliveryPartner {
    void deliverOrder(String orderId, String address);
}

public class BikeDelivery implements DeliveryPartner {
    public void deliverOrder(String orderId, String address) {
        System.out.println("By bike: " + orderId);
    }
}
```

`DeliveryService` 只依賴 `DeliveryPartner` 介面，未來加 `RobotDelivery` 完全不影響既有程式碼。

**面試重點**：Realization 是 Polymorphism 之所以能運作的底層機制——多個 Class 實現同一介面，才能被當作同一種型別統一呼叫。UML 上 Inheritance 用「實線 + 空心三角箭頭」，Realization 用「虛線 + 空心三角箭頭」表示。

---

## 📊 面試必考比較表

### Encapsulation vs Abstraction（史上最常被考、也最常被答錯的一題）

| | Encapsulation（封裝） | Abstraction（抽象化） |
|---|---|---|
| 關注點 | **如何保護資料**（How） | **隱藏什麼複雜度**（What） |
| 手段 | 存取修飾詞（`private`/`public`）、getter/setter | 只暴露必要方法，隱藏內部流程／實作 |
| 層級 | 資料層級 | 設計／流程層級 |
| 例子 | `BankAccount.balance` 設 `private` | `FoodOrder.placeOrder()` 隱藏五個內部步驟 |
| 一句話 | 「別人不能亂改我的資料」 | 「別人不需要知道我怎麼做到的」 |

### 四種物件關係總表（耦合強度排序）

| 關係 | 中文 | 關係強度 | 生命週期 | 程式碼特徵 |
|------|------|----------|----------|------------|
| Dependency | 依賴 | 最弱 | 無（用完即丟） | 物件只出現在方法參數 |
| Association | 關聯 | 弱 | 各自獨立 | 物件存成欄位，但雙方各自建立 |
| Aggregation | 聚合 | 中 | 獨立（弱擁有） | 子物件從外部傳入（建構子/setter） |
| Composition | 組合 | 強 | 綁定（強擁有） | 子物件在建構子內部 `new` |

### Method Overloading vs Overriding

| | Overloading（多載） | Overriding（覆寫） |
|---|---|---|
| 別名 | Compile-time Polymorphism | Runtime Polymorphism |
| 發生位置 | 同一個 Class（或父子類別間），方法名相同 | 子類別 / 介面實作類別 |
| 決定時機 | 編譯期，依參數列表決定 | 執行期，依實際物件型別動態決定（dynamic dispatch） |
| 關鍵字 | 無特殊需求 | Java：`@Override`（建議）；C#：`override`（必要） |

### Interface vs Abstract Class（C# / 一般 OOP 通用）

| | Interface | Abstract Class |
|---|---|---|
| 可否有欄位 | 不行（C#/Java 都不行，只能有常數） | 可以 |
| 可否有建構子 | 不行 | 可以 |
| 可否有預設實作 | 可以（C# 8+ / Java 8+ 的 default method） | 可以（non-abstract 方法） |
| 多重繼承 | 一個類別可實作多個 Interface | 一個類別只能繼承一個 Abstract Class |
| 適用時機 | 定義「能力／contract」，例如 `IComparable`、`IDisposable` | 有共用狀態＋部分共用邏輯，且是同一種東西的 is-a 階層 |

---

## 🔄 C# 語法對照（面試常考的 Java/C# 差異）

原文範例是 Java，但面試若以 C# 進行，以下差異務必弄清楚：

| 概念 | Java | C# |
|------|------|-----|
| 繼承類別 + 實作介面 | `extends` 用於類別、`implements` 用於介面，兩者語法不同 | 統一用 `:`，用逗號分隔多個介面，例如 `class Dog : Animal, IPet` |
| 呼叫父類建構子 | `super(...)` | `base(...)` |
| 方法預設是否可覆寫 | **預設 virtual**（除非標 `final`） | **預設 sealed（不可覆寫）**，父類方法必須明確標 `virtual`/`abstract` |
| 標記覆寫 | `@Override`（僅是編譯期檢查用的 annotation，不寫也能覆寫） | `override`（**必要關鍵字**，不寫會變成「method hiding」而非覆寫，是常見面試陷阱） |
| 介面預設實作 | `default` 關鍵字（Java 8+） | 直接在 interface 內寫方法本體（C# 8+） |
| 介面命名慣例 | 無強制慣例（如 `NotificationService`） | 慣例上介面名稱以 `I` 開頭（如 `INotificationService`） |

> ⚠️ **這題超容易在面試被問**：「C# 的 override 跟 Java 有什麼不同？」
> 答案核心：**C# 要求「顯式標記」——父類要標 `virtual`，子類要標 `override`，這是 C# 特意的設計，避免不小心覆寫了父類別的方法（意外破壞多型）；Java 則是預設所有 instance method 皆可覆寫，比較寬鬆。**

---

## 🎯 面試常見問題與建議回答

### OOP 基礎觀念

**Q1：請簡述 OOP 四大特性？**
> Encapsulation（封裝資料、控制存取）、Abstraction（隱藏複雜度、暴露必要介面）、Inheritance（子類別重用父類別的資料與行為）、Polymorphism（同一操作，不同物件有不同表現）。

**Q2：Encapsulation 跟 Abstraction 差在哪？**
> Encapsulation 管的是「資料怎麼被存取」（private + getter/setter），Abstraction 管的是「複雜度要不要被看到」（暴露 `placeOrder()`，隱藏五個內部步驟）。兩者常常一起出現，但解決的問題不同：一個是安全性/資料完整性，一個是使用簡易性。

**Q3：為什麼欄位要設 `private` 而不是 `public`？**
> 讓 Class 自己掌控資料何時、如何被修改，可以在方法內加驗證邏輯（例如餘額不能為負），避免物件進入不合法狀態；同時也讓未來要換內部實作時，外部呼叫端完全不受影響（資訊隱藏）。

### 繼承與多型

**Q4：什麼時候該用 Inheritance？什麼時候不該用？**
> 只有明確、穩固的 **is-a** 關係才該用繼承（`Intern is an Employee`）。如果只是想重用程式碼，但關係並不自然，繼承會讓程式碼變得僵化、脆弱（父類別一改，所有子類別都受影響），這種情況應該優先考慮 Composition。

**Q5：什麼是「Favor Composition over Inheritance」？為什麼？**
> 繼承是編譯期就固定的強耦合（is-a），且大多數語言（Java/C#）類別只能單一繼承，階層一旦設計錯誤很難調整；組合則是透過持有介面型別的物件、在執行期決定實際行為，可以彈性抽換依賴，更符合開放封閉原則，單元測試時也更容易 mock 掉依賴的介面。

**Q6：Overloading 跟 Overriding 的差異？**
> Overloading 是編譯期依參數列表決定要呼叫哪個同名方法（同一 Class 內）；Overriding 是子類別對父類別／介面方法提供新實作，執行期依實際物件型別動態決定要跑哪一版（dynamic dispatch）。

**Q7：多型在實務上解決了什麼問題？**
> 讓呼叫端可以寫「一份程式碼」處理「多種型別」，新增新的實作類別時完全不用修改既有呼叫端邏輯（開放封閉原則）。文章中的 `PaymentMethod`、`NotificationService`、`DeliveryPartner` 都是同一個模式的重複應用。

### 物件關係

**Q8：Association、Aggregation、Composition、Dependency 怎麼分辨？**
> 看兩件事：**(1) 關係維持多久**——只在方法參數中短暫出現是 Dependency；存成欄位是 Association/Aggregation/Composition。**(2) 生命週期是否綁定**——子物件從外部傳入、可獨立存在是 Aggregation；子物件在建構子內部 `new` 出來、隨父物件生滅是 Composition。

**Q9：Aggregation 跟 Composition 在程式碼上怎麼一眼看出差異？**
> 看物件「在哪裡被建立」。`school.addTeacher(teacher)`——teacher 是外部建立好傳進來的，是 Aggregation。`this.engine = new Engine();` 寫在 Car 的建構子裡——是 Composition。

**Q10：Realization 跟 Inheritance 有什麼不同？**
> Inheritance 是類別繼承類別（可以繼承「實作」），Realization 是類別實現介面（只繼承「契約」，沒有任何實作）。UML 圖上，Inheritance 是實線 + 空心三角箭頭，Realization 是虛線 + 空心三角箭頭。

**Q11：Interface 跟 Abstract Class 該怎麼選？**
> 如果只是要定義一種「能力」或「契約」（例如「可以被序列化」「可以被比較」），且未來可能有多個不相關的類別都要實作 → Interface。如果一群類別本質上是同一種東西、有共用的狀態與部分共用邏輯（例如 `Employee` 家族共用 `name`、`showDetails()`）→ Abstract Class。也可以兩者併用：用 Abstract Class 提供共用邏輯，再實作額外的 Interface 補充能力。

### 綜合／設計題

**Q12：如果要你設計一個「多種付款方式」的系統，你會怎麼運用 OOP？**
> 定義 `PaymentMethod` 介面，各付款方式（信用卡、UPI、PayPal）各自實作 `pay()`；`Checkout` 類別依賴介面而非具體類別（依賴反轉），新增付款方式只需新增一個實作類別，不改動既有程式碼——這同時展示了 Polymorphism、Realization、OCP、DIP 四個概念的綜合應用。

**Q13：為什麼依賴介面（Interface）而不是依賴具體類別，對測試有幫助？**
> 依賴介面時，測試可以傳入假的實作（Mock/Stub）取代真正的資料庫、金流 API 等外部服務，不需要啟動真實環境就能驗證邏輯是否正確，這是依賴反轉原則（DIP）與可測試性的核心關聯。

---

## ⚠️ 常見面試陷阱／誤區

1. **把 Encapsulation 和 Abstraction 講成同一件事**——這是最容易被扣分的地方，務必用「How to access」vs「What to expose」這組對比來回答。
2. **以為 Aggregation 和 Composition 是同一種東西**——兩者都是 has-a，差別在生命週期是否綁定，务必能舉出程式碼層級的判斷依據（物件在哪裡被 `new`）。
3. **以為多型只有 Runtime Polymorphism**——別忘了 Overloading（Compile-time）也是多型的一種。
4. **以為 Interface 完全不能有實作**——Java 8+ / C# 8+ 都支援 Interface 的 `default` 方法（預設實作），這個「新知識」常被拿來考近幾年是否有跟上語言演進。
5. **搞混 C# 的 override 規則**——以為和 Java 一樣「預設都能覆寫」，忘記 C# 父類別必須明確標 `virtual`，子類別必須標 `override`，否則會變成不易察覺的 method hiding。
6. **只因為想重用程式碼就用繼承**——沒有真正的 is-a 關係卻硬用繼承，是資深面試官很愛追問的「你怎麼判斷該不該用繼承？」的破綻題。

---

## 🎁 加碼複習：SOLID 原則快速對照（常與 OOP 基礎一起被問）

面試常常在問完 OOP 四大特性後，緊接著問 SOLID，剛好可以無縫銜接：

| 原則 | 全名 | 一句話 | 與本文的呼應 |
|------|------|--------|--------------|
| **S** | Single Responsibility | 一個 Class 只該有一個變更的理由 | `BankAccount` 只管帳戶邏輯，不管發通知 |
| **O** | Open/Closed | 對擴充開放，對修改封閉 | 新增 `WalletPayment` 不用改 `Checkout` |
| **L** | Liskov Substitution | 子類別要能無痛替換父類別使用 | `FullTimeEmployee`/`Intern` 都能當 `Employee` 用而不出錯 |
| **I** | Interface Segregation | 介面要小而專一，不強迫實作用不到的方法 | `NotificationService` 只定義 `sendMessage()`，不夾帶無關方法 |
| **D** | Dependency Inversion | 依賴抽象（Interface），不依賴具體實作 | `AlertManager` 依賴 `NotificationService` 介面，不依賴 `EmailNotification` |

> 💡 **面試連結技巧**：文章中反覆出現的 `NotificationService` / `PaymentMethod` / `DeliveryPartner` 範例，其實都是同一個模式——**「定義 Interface → 多個 Class 實現 → 呼叫端只依賴 Interface」**。這個模式一次展示了 Realization、Polymorphism、OCP、DIP 四個概念，是面試中證明你「不只會背名詞，還理解概念如何互相支撐」的最佳素材。

---

## 🎯 資深工程師評論

### 整體評價

這篇文章的價值在於「用最白話的方式，把 UML 教科書會用很硬的術語講的東西，講成新手也能懂的敘事」。12 個概念的分類方式（建構元素 → 四大支柱 → 物件關係）邏輯清楚，很適合當作 OOP 知識的「骨架」，之後再往上長肉（設計模式、SOLID、DDD）。

### 值得肯定的地方

**用「強度排序」理解四種物件關係，是這篇文章隱藏最深、但最有用的一條線索。** Dependency → Association → Aggregation → Composition 這個順序，本質上是在回答同一個問題的不同答案：「這兩個物件的關係要維持多久、多緊密？」用生命週期和程式碼中物件建立的位置（方法參數 / 欄位＋外部傳入 / 欄位＋內部建立）來判斷，比死背 UML 菱形符號實用得多，這也是為什麼本筆記特別把這個判斷技巧提煉出來獨立成一段。

**每個概念都搭配「一句話總結」的寫法，非常適合面試情境。** 面試官問「什麼是 XXX」時，能不能在 10 秒內講出一句精準的定義，往往比長篇大論更能展現理解深度。這也是本筆記在 Q&A 區塊刻意保持「先講重點，再補充」寫法的原因。

### 這份原文美中不足的地方

**Composition 段落缺漏，是原文最大的硬傷。** 一篇宣稱要講「12 個概念」的文章，卻在關鍵的第 10 個概念上開了預告支票、卻沒有兌現，這對讀者（尤其是面試準備者）是很大的風險——如果沒發現這個缺口，很可能會在面試被問到「Aggregation 跟 Composition 差在哪」時，只能講出 Aggregation 那一半的答案。這也是為什麼看到這種第二手學習素材時，**多方查證、發現內容缺口並主動補齊，是比單純照抄筆記更重要的能力**。

**文章完全沒有觸及 CAP 語言層級的差異（例如 Java vs C# 的 override 規則），對於準備 C# 職缺面試的讀者來說是個資訊落差。** 這也是本筆記特別加上「C# 語法對照」章節的原因——概念的理解是通用的，但面試官很可能會順著概念問「那在你熟悉的語言裡怎麼寫」，這時候語言層級的細節差異（尤其是 `virtual`/`override` 這種容易讓 Java 背景的人在 C# 面試中失分的地方）就非常關鍵。

**物件關係（Association/Aggregation/Composition/Dependency/Realization）的介紹偏向記憶型知識，缺少「為什麼要分這麼細」的實務動機。** 這五種關係在 LLD（Low-Level Design）面試中最大的實用價值，其實是幫助你在畫 Class Diagram、討論系統設計時，**用精確的詞彙溝通耦合程度**，進而推導出「這裡該不該抽 Interface」「這個依賴該不該注入」等設計決策。單純背關係的定義而不連結到「這對我的系統設計決策有什麼影響」，容易流於背誦，這也是本筆記在每個關係後面都補充「判斷關鍵字」與程式碼特徵的原因。

### 面試準備建議

1. **先確保四大支柱（Encapsulation/Abstraction/Inheritance/Polymorphism）能各用一句話 + 一個生活化例子清楚講出來**，這是任何 OOP 面試的地基，講不清楚會直接扣分。
2. **物件關係的部分，練習「看程式碼判斷關係」而不是「背定義」**——面試官很常直接丟一段程式碼問「這是 Association 還是 Composition？」，能不能立刻指出「這個物件是從建構子外部傳入的」才是關鍵能力。
3. **準備好把 OOP 基礎「接到」SOLID 和設計模式**——面試很少停在「什麼是繼承」，通常會追問「那你覺得這樣設計違反了 SOLID 的哪個原則？」，本筆記加碼的 SOLID 對照表就是為了這個銜接準備的。
4. **如果應徵 C# 職缺，務必準備好 `virtual`/`override` 這個常見的 Java vs C# 差異題**，這是很多從 Java 背景轉 C# 的人會被抓到的知識落差。

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性（面試準備） | ⭐⭐⭐⭐☆（扣分在 Composition 缺漏） |
| 深度 | ⭐⭐⭐☆☆ |
| 新穎性 | ⭐⭐☆☆☆（皆為經典 OOP 概念） |
| 適合對象 | 初階工程師、準備 OOP / LLD 面試者 |

---

## 📝 面試前 5 分鐘速查表

| 概念 | 一句話定義 | 關鍵字 |
|------|-----------|--------|
| Class | 定義資料與行為的藍圖 | 模板、不佔實例記憶體語意 |
| Object | 從 Class 建立的實際實例 | 獨立狀態 |
| Interface | 只定義「必須做什麼」的契約 | What not How |
| Encapsulation | 用 private + 方法控制資料存取 | How to access |
| Abstraction | 隱藏複雜度，只暴露必要操作 | What to expose |
| Inheritance | 子類別重用父類別的資料與行為 | is-a 關係 |
| Polymorphism | 同一呼叫，不同物件不同表現 | Overloading(編譯期) / Overriding(執行期) |
| Association | 物件互相知道，各自獨立 | 一般關聯，無擁有關係 |
| Aggregation | 弱 has-a，子物件可獨立存在 | 外部建立、傳入 |
| Composition | 強 has-a，生命週期綁定 | 建構子內部 `new` |
| Dependency | 暫時使用，用完即丟 | 只存在於方法參數 |
| Realization | Class 實現 Interface 的契約 | 多型的底層機制 |

**記憶口訣**：
> 「類別是藍圖，物件是實體；介面訂契約，類別來履行（Realization）。
> 封裝管存取，抽象管複雜；繼承重邏輯，多型看表現。
> 關聯最鬆散，聚合能獨立；組合綁生死，依賴最短暫。」
