# 《資料庫基礎：SQL vs NoSQL — 何時用哪個》繁體中文總結與評論

> 原文：Database Basics: SQL vs NoSQL — When to Use What

---

## 📋 文章總結

這篇文章的核心觀點是：**你選擇的資料庫會塑造一切——資料模型、查詢方式、擴展策略，甚至凌晨三點的 debug 現場**。沒有完美的資料庫，只有最適合你當下需求的選擇。

---

### 資料庫的三件事

任何資料庫都在做這三件事：

1. **儲存資料**：可靠地持久化資訊
2. **取回資料**：快速找到你需要的東西
3. **維護完整性**：確保資料始終一致且有效

SQL 與 NoSQL 用不同的方式達成這三件事，沒有優劣之分，只有場景適不適合。

---

### SQL 資料庫：結構化的強大工具

**設計哲學**：「先定義結構，永遠執行它，查詢任何東西。」

SQL（關聯式）資料庫自 1970 年代就存在，以資料表儲存資料，並透過外鍵建立表與表之間的關聯。

#### ACID 保證

SQL 最重要的特性是 **ACID**：

| 屬性 | 說明 |
|------|------|
| **Atomicity（原子性）** | 交易全部成功或全部失敗，絕不半途而廢 |
| **Consistency（一致性）** | 資料永遠符合你定義的規則 |
| **Isolation（隔離性）** | 並發交易互不干擾 |
| **Durability（持久性）** | 已提交的資料在崩潰後仍然存在 |

銀行轉帳的例子最能說明這件事：扣款和入帳要嘛都成功，要嘛都不發生。

#### 優點與缺點

| ✅ 優點 | ❌ 缺點 |
|---------|---------|
| 強大的查詢能力（JOIN、聚合、子查詢） | Schema 剛性，修改需要 migration |
| 資料完整性保證（外鍵、約束、交易） | 垂直擴展限制（受單機上限約束） |
| 成熟的生態系統（50+ 年工具和知識） | 物件與資料表的阻抗不匹配 |
| ACID 保證 | 大量寫入吞吐量不是強項 |
| 可以查詢設計當時未預期的問題 | |

**作者的預設推薦：PostgreSQL**，處理 90% 的使用場景都綽綽有餘。

---

### NoSQL 資料庫：彈性的大家庭

NoSQL（"Not Only SQL"）在 2000 年代崛起，用來解決 SQL 難以應對的問題：超大規模、彈性 Schema、以及特殊的資料存取模式。

**NoSQL 不是一種東西，而是四種不同的資料庫類型。**

---

#### 1. 文件資料庫（Document Databases）
> MongoDB、CouchDB

**設計哲學**：「以應用程式使用資料的方式儲存資料。」

將資料以 JSON 格式的文件儲存，相關資料放在同一份文件中，不需要 JOIN。

```json
{
  "_id": "507f1f77bcf86cd799439011",
  "username": "john_doe",
  "profile": { "bio": "Backend engineer", "location": "San Francisco" },
  "posts": [
    { "title": "My First Post", "tags": ["intro"] }
  ],
  "settings": { "notifications": true, "theme": "dark" }
}
```

**適合**：Schema 多變、資料有層次結構（巢狀物件）、讀取密集且存取模式固定、快速原型開發。

**不適合**：多對多關聯、需要跨文件查詢、資料完整性要求高、查詢模式未知。

---

#### 2. Key-Value 儲存（Key-Value Stores）
> Redis、DynamoDB

**設計哲學**：「用 Key 查找，快到不行。其他都不重要。」

最簡單的模型：一個 Key 對應一個 Value。

**Redis 的強項**：
- `setex`：帶 TTL 的 Session 儲存
- `incr`：Thread-safe 的原子計數器（流量限制）
- `zadd` / `zrevrange`：排行榜
- `lpush` / `rpop`：訊息佇列

**適合**：快取計算結果、Session 儲存、Rate Limiting、排行榜、即時資料。

**不適合**：複雜查詢、資料關聯、大型資料集（Redis 全存記憶體）、當作主要資料庫。

---

#### 3. 寬欄資料庫（Wide-Column Stores）
> Cassandra、HBase

**設計哲學**：「寫入一切，以已知模式讀取，無限擴展。」

為超大規模設計，每列可以有不同的欄位，欄位以 Column Family 分組。

**關鍵限制**：查詢**必須**透過 Partition Key，否則極慢或直接禁止。這意味著你必須以查詢模式來設計資料表，而非以資料結構。

**適合**：每秒百萬次寫入、時序資料（IoT 感測器、Log、Metrics）、跨資料中心部署、線性擴展需求。

**不適合**：Ad-hoc 查詢、交易支援、中小型規模（過度設計）。

---

#### 4. 圖資料庫（Graph Databases）
> Neo4j、Neptune

**設計哲學**：「關係是一等公民，不是事後才想到的東西。」

以節點（Node）和邊（Edge）儲存資料，專為高度連結的資料設計。

```cypher
// 找出與 Alice 相互追蹤的人
MATCH (alice:User {name: 'Alice'})-[:FOLLOWS]->(mutual)<-[:FOLLOWS]-(bob:User {name: 'Bob'})
RETURN mutual.name

// 推薦 Alice 追蹤的人喜歡的貼文
MATCH (alice:User {name: 'Alice'})-[:FOLLOWS]->(friend)-[:LIKES]->(post)
WHERE NOT (alice)-[:LIKES]->(post)
RETURN post.title, COUNT(friend) as friend_likes
ORDER BY friend_likes DESC
```

**適合**：社群網路（朋友、追蹤）、推薦引擎、詐欺偵測、知識圖譜、基礎設施依賴分析。

**不適合**：簡單 CRUD、聚合分析、超大規模水平擴展（比 Cassandra 難）。

---

### 決策框架：五個問題幫你選擇

| 問題 | 答案 → 選擇 |
|------|------------|
| **資料結構化程度？** | 高度結構化 → SQL；多變巢狀 → Document；簡單 Key 存取 → Key-Value；關係密集 → Graph |
| **查詢模式？** | 複雜 Ad-hoc → SQL；固定模式取 ID → NoSQL；遍歷關聯 → Graph；時序 Append → Wide-Column |
| **資料完整性要求？** | 關鍵（金錢、庫存）→ SQL ACID；最終一致性可接受 → NoSQL；兩者都要 → SQL 主庫 + NoSQL 快取 |
| **規模？** | < 1TB、< 10K req/s → SQL 就夠；超大寫入、PB 級 → Wide-Column；超大讀取快取 → Key-Value |
| **團隊經驗？** | 有 SQL 背景 → SQL；新手 → PostgreSQL（文件最好、技能最通用） |

---

### 常見的多資料庫組合

生產系統通常不只用一種資料庫：

- **典型 Web 應用**：PostgreSQL（主資料）+ Redis（快取/Session）
- **社群平台**：PostgreSQL + Neo4j（好友關係）+ Redis
- **IoT / 分析平台**：PostgreSQL + Cassandra（時序資料）+ Redis

---

### 作者的誠實建議

1. **新專案預設選 PostgreSQL**：它原生支援關聯資料，JSONB 欄位讓它幾乎也能當 Document DB 用，擴展上限遠超大多數專案需求。
2. **PostgreSQL + Redis 涵蓋了 80% 的應用場景**。
3. **不確定時，就從 PostgreSQL 開始**，需要時再加入特化資料庫，但從一開始就選錯要遷移？那才是真正的痛苦。

---

### 作者踩過的坑

| 錯誤 | 教訓 |
|------|------|
| 「NoSQL 比較快」而選了 MongoDB | 結果做了大量關聯，最後在應用層手工 JOIN。應該用 PostgreSQL |
| 太晚引入 Redis | 花了好幾週優化 DB 查詢，加一天 Redis 快取改善了 10 倍 |
| 從第一天就用四種資料庫 | 花更多時間同步資料，而不是開發功能。Keep it simple |
| 忽視索引 | 慢查詢不是資料庫的問題，是索引的問題。學會用 `EXPLAIN ANALYZE` |

---

## 🎯 資深工程師評論

### 整體評價

這篇文章做到了一件很難的事：**把「視情況而定」翻譯成可執行的判斷框架**。大多數談論 SQL vs NoSQL 的文章不是淪為教科書條列，就是流於「各有優缺點，自己判斷吧」的廢話。這篇的決策框架雖然不完美，但足夠實用。

---

### 值得肯定的地方

**「PostgreSQL 是預設答案」這個立場非常有勇氣，也非常正確。**

很多開發者在選資料庫時容易受到技術熱點影響，看到 MongoDB 或 Cassandra 的使用案例就被吸引，卻忽略了那些案例背後通常是 Google、Netflix 規模的問題。PostgreSQL 的 JSONB 欄位已經讓它在彈性 Schema 的場景中表現得非常好，而它的 ACID 保證和成熟生態系統是大多數商業應用真正需要的東西。

**「索引問題不是資料庫問題」這個觀點直指很多性能 bug 的根源。**

很多工程師在遇到慢查詢時，第一反應是換資料庫，但 90% 的情況下，問題出在沒有適當索引，或是索引設計不符合查詢模式。學會 `EXPLAIN ANALYZE` 應該是後端工程師的基本功，而不是選項。

**「太晚使用 Redis」這個悔恨故事說明了快取的投資報酬率之高。**

快取不只是性能最佳化工具，更是一種架構決策。早點引入 Redis 做 Session、做熱點資料快取、做 Rate Limiting，往往比花大量時間調校主資料庫的查詢計劃更有效。

---

### 可以更深入的地方

**文章對 CAP 定理的探討完全缺席。**

在談論 NoSQL 資料庫的取捨時，無法迴避的核心理論是 CAP 定理：在分散式系統中，Consistency（一致性）、Availability（可用性）、Partition Tolerance（分區容錯）三者只能同時保證兩個。Cassandra 和 DynamoDB 選擇 AP（可用性優先），PostgreSQL 傳統上選擇 CP（一致性優先）。了解這個框架能讓讀者更深刻地理解為什麼某些資料庫在面對網路分區時行為差異如此巨大。

**「文件資料庫的 Many-to-Many 很麻煩」這個痛點說得不夠深。**

文章提到了這個問題，但沒有說明為什麼。核心原因是：文件資料庫通過「嵌入（embedding）」避免 JOIN，但 Many-to-Many 意味著同一份資料需要在多個文件中冗餘存在，一旦這份資料需要更新，你得更新所有持有它的文件，這不只是性能問題，更是資料一致性問題。在 SQL 中，這個問題由外鍵和正規化自動解決。

**Wide-Column 一節應該強調「以查詢設計資料表」的衝擊。**

Cassandra 要求你根據查詢模式設計 Schema，這和 SQL 「以資料語意設計 Schema，查詢可以自由組合」的思維是根本性的顛覆。在實務上，這意味著同一份邏輯資料可能需要存多份不同結構的 Table 來支援不同的查詢路徑，也就是「以空間換查詢速度」。這個設計決策對維護成本的影響很大，值得特別說明。

---

### 給台灣電商/SaaS 工程師的補充觀察

台灣常見的系統型態是商店後台 + 前台 + 金物流整合，在這個脈絡下：

1. **PostgreSQL + Redis 幾乎是標準配置**，且通常已經夠用。在達到真正需要 Cassandra 或 Sharding 的規模之前，先把 PostgreSQL 的索引、查詢計劃、連線池調好，效益更高。

2. **MongoDB 在快速迭代期很誘人，但要小心 Schema 失控**。沒有 migration 的代價是：六個月後你的 Collection 裡可能有五種不同結構的文件共存，查詢邏輯裡充滿防禦性的 `null` 檢查。如果選 MongoDB，請認真做 Schema Validation。

3. **Redis 的最常見使用場景在台灣後端幾乎是必備**：購物車、Session、API Rate Limiting、SKU 熱點資料快取。不應該被當作「以後性能不夠再加」的事後手段，而是從一開始就納入架構考量。

4. **圖資料庫在台灣的使用場景相對有限**，除非你正在建立推薦系統或需要複雜的朋友關係遍歷。大多數情況下，PostgreSQL 的遞迴 CTE（`WITH RECURSIVE`）或 ltree 擴充就已足夠應付中等複雜度的層級關係查詢。

---

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐☆☆ |
| 新穎性 | ⭐⭐☆☆☆ |
| 適合對象 | 初階至中階後端工程師 |

**這是一篇很好的「打底」文章**，適合剛開始設計系統或還不確定資料庫選型標準的工程師。如果想更系統性地深入，可以延伸閱讀：

- *Designing Data-Intensive Applications* — Martin Kleppmann（目前業界公認最完整的分散式資料系統教科書）
- *Database Internals* — Alex Petrov（如果想理解各類資料庫引擎的底層原理）
- PostgreSQL 官方文件的 [Query Planning](https://www.postgresql.org/docs/current/runtime-config-query.html) 章節（索引和查詢計劃的實務指南）
