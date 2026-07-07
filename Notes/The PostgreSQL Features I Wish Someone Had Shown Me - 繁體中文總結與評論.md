# 《我希望三年前就有人告訴我的 PostgreSQL 功能》繁體中文總結與評論

> 原文：The PostgreSQL Features I Wish Someone Had Shown Me Three Years Ago

---

## 📋 文章總結

週二早上 9:14，公司最大的客戶打了電話來——不是寄信，是直接打電話。他們的晨間儀表板已經 Timeout 二十分鐘了，而自家的狀態頁卻顯示一切正常，這讓情況更加難堪：問題對開發團隊來說是隱形的，對客戶而言卻無比明顯。

作者很快就抓到問題：一個他六個月前寫的輪詢（polling）迴圈，每五秒檢查一次資料表有沒有新任務。在早上五十個使用者同時擠進儀表板的負載下，資料庫被大量並行讀取淹沒。他花了四十分鐘，用更好的索引和更精簡的查詢把它修好，載入時間從 800ms 降到可以接受的程度，然後就把這件事當成一次勝利。

三週後，在一次 Code Review 裡，從資料庫顧問公司轉來的資深工程師 **Ihsan**，盯著這個輪詢迴圈看了大約四秒。

> 「你為什麼不用 LISTEN/NOTIFY？」她問。

作者完全不知道那是什麼。

那一刻，開啟了往後一整個月的對話——Ihsan 一件一件地，向作者展示了他錯過的東西。作者已經寫了三年的 PostgreSQL Query，原本以為自己懂這個工具，結果他真正懂的，只是這個工具極薄的一小片而已。

這篇文章的核心主張是：**「Good enough（夠用就好）」其實是一個真實存在的工程類別**。多數時候，我們並不是在「該用 Postgres 還是該上專門工具」之間做深思熟慮的抉擇，而是根本不知道 Postgres 原生就能做到這些事，於是預設走向自己已經熟悉的做法——輪詢、Redis、應用層過濾……這是「不知道自己不知道」的問題，而不是選錯技術的問題。

以下是 Ihsan 一個月內教會作者的五個功能，每一個背後都有一次真實的生產事故。

---

### 1. LISTEN/NOTIFY — 藏在檯面上的事件系統

作者寫的輪詢迴圈，是每個後端工程師都寫過的「顯而易見的解法」：檢查資料表、撈出待處理的資料列、處理它。問題是，它在每次迭代都做著「什麼都沒變」的白工，而且會產生互相競爭的讀取，在高負載下堆疊成災難。

Ihsan 讓作者看見：Postgres 從 9.0 版開始，就內建了一套原生的 pub/sub 機制。

```sql
-- 任何 session 都可以訂閱一個命名頻道
LISTEN task_created;

-- 任何其他 session 都可以廣播到這個頻道
NOTIFY task_created, '{"task_id": 8821, "priority": "high"}';
```

監聽端會在幾毫秒內收到 payload——沒有輪詢間隔、沒有雪崩式的驚群效應、沒有任何「什麼都沒變」時的多餘讀取。搭配一個 Trigger，新任務就能自己廣播自己：

```sql
CREATE OR REPLACE FUNCTION notify_task_created()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_notify('task_created', row_to_json(NEW)::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER on_task_insert
AFTER INSERT ON tasks
FOR EACH ROW EXECUTE FUNCTION notify_task_created();
```

C#（使用 Npgsql）監聽端範例：

```csharp
public sealed class TaskListener(string connectionString)
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        connection.Notification += (_, e) => ProcessTaskNotification(e.Payload);

        await using var listenCmd = new NpgsqlCommand("LISTEN task_created", connection);
        await listenCmd.ExecuteNonQueryAsync(cancellationToken);

        // 持續等待通知；每 90 秒逾時一次順便確認連線仍然存活
        while (!cancellationToken.IsCancellationRequested)
        {
            await connection.WaitAsync(TimeSpan.FromSeconds(90), cancellationToken);
        }
    }

    private static void ProcessTaskNotification(string payload)
    {
        // 解析 payload 並處理任務
    }
}
```

**核心思想**：輪詢只是一個「大部分時間都在做白工」的迴圈。

#### 📊 實際效果

作者用當初造成週二事故的完全相同負載條件重跑了兩種實作：50 個並行使用者、每小時新增 200 筆任務。

| 實作方式 | 平均回應時間 | 尖峰 |
|---------|------------|------|
| 輪詢（Polling） | 340ms | 任務恰好在輪詢週期前建立時，飆到 1.1 秒 |
| LISTEN/NOTIFY | 18ms | 同樣負載下維持平穩 |

Dashboard 本身的查詢完全沒有改動，所有的改善都來自「移除了原本一直在跟它搶資源的輪詢讀取」。

**誠實的限制**：LISTEN/NOTIFY 不是一個持久化的訊息佇列。如果 NOTIFY 觸發時沒有任何 listener 連著，訊息就會直接消失。如果你需要保證送達或高吞吐量的事件串流，還是得靠 Kafka 或 RabbitMQ。但對於一個「本來就已經跑在 Postgres 上」的系統內部事件協調，這個做法可以直接砍掉一整層基礎設施。

---

### 2. Advisory Locks — 不需要鎖資料表的分散式協調

儀表板事故過後兩個月，團隊把任務處理器擴充到三個 instance 上運行——結果變成三個 instance 同時搶著處理同一批任務，寫入互相衝突的結果。

當下的權宜之計是一個 Redis 鎖：`SETNX` 搭配 60 秒過期時間，再加一個心跳（heartbeat）程序維持鎖的存活。這方法是有效的，但也讓一個原本不需要 Redis 的服務，硬生生多了一個必要依賴。而且往後好幾個月，團隊持續在處理「鎖卡住（stale lock）」的事故：某個程序當掉，留下一個 Redis 鎖，下一次執行就得一直卡著，直到過期時間到才能繼續。

作者把這件事提給 Ihsan，她問：你有沒有研究過 Advisory Locks？

PostgreSQL 的 Advisory Lock 是完全儲存在 Postgres 裡、由應用程式自行定義的鎖。你用任意一個整數當作鎖的識別碼，資料庫就會在所有連線的 session 之間協調存取權。

```sql
-- 非阻塞：拿到鎖回傳 true，被別人持有時回傳 false
SELECT pg_try_advisory_lock(12345);

-- 明確釋放鎖
SELECT pg_advisory_unlock(12345);
```

Session 層級的 Advisory Lock 會在連線關閉時自動釋放。如果程序在工作跑到一半當掉，鎖也會跟著消失——沒有卡住的殘留紀錄、沒有跟過期時間賽跑的競態、不需要心跳程序。

```csharp
public async Task<bool> RunExclusiveJobAsync(
    NpgsqlConnection connection, string jobName, CancellationToken cancellationToken)
{
    await using var lockIdCmd = new NpgsqlCommand("SELECT hashtext($1)::bigint", connection);
    lockIdCmd.Parameters.AddWithValue(jobName);
    var lockId = (long)(await lockIdCmd.ExecuteScalarAsync(cancellationToken))!;

    await using var tryLockCmd = new NpgsqlCommand("SELECT pg_try_advisory_lock($1)", connection);
    tryLockCmd.Parameters.AddWithValue(lockId);
    var acquired = (bool)(await tryLockCmd.ExecuteScalarAsync(cancellationToken))!;

    if (!acquired)
        return false; // 代表另一個 instance 正在跑，這是預期行為，不是錯誤

    try
    {
        await ProcessJobAsync(connection, cancellationToken);
        return true;
    }
    finally
    {
        await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock($1)", connection);
        unlockCmd.Parameters.AddWithValue(lockId);
        await unlockCmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
```

團隊把 Redis 從這個服務的依賴清單中整個移除。鎖卡住的事故立刻停止——不是因為團隊「修好了」它，而是 Session 層級鎖「自我修復」的特性，讓這種狀況在結構上根本不可能發生。

**真正的取捨**：Advisory Lock 把你的分散式協調機制，直接綁在資料庫的可用性上。如果 Postgres 掛了，鎖機制也跟著掛了。對這個案例來說無所謂，因為這個工作本來就得存取資料庫——如果 Postgres 不可用，工作本來就跑不了。

---

### 3. JSONB 運算子 — 我在應用程式碼裡做的事，資料庫其實早就能做

差不多同一時期，Ihsan 隨口提了一句：你該去看看 JSONB 的 containment（包含）運算子。她提起這件事的語氣，就像在提一個你不知道存在的鍵盤快捷鍵一樣——不是在酸你，就只是順口一提。

大多數用過 JSONB 的開發者都知道 `->` 和 `->>` 可以取出欄位，比較少人知道的，是一整組可以在 JSON 結構「內部」直接過濾的運算子：

```sql
-- @> 「包含」：找出 JSON 內含這個子集合的資料列
SELECT * FROM users
WHERE preferences @> '{"notifications": {"email": true}}';

-- ? 「鍵是否存在」
SELECT * FROM products WHERE attributes ? 'color';

-- ?| 「任一鍵存在」
SELECT * FROM products WHERE attributes ?| array['color', 'size'];

-- ?& 「所有鍵都存在」
SELECT * FROM products WHERE attributes ?& array['color', 'size'];

-- jsonb_set：只更新巢狀結構裡的一個欄位，不必整份文件重寫
UPDATE users
SET preferences = jsonb_set(preferences, '{notifications, email}', 'false')
WHERE id = 42;
```

`@>` 這個運算子改變了作者寫查詢的方式。他原本的做法是把整個 JSONB 欄位撈進應用程式碼再過濾——把全部資料透過網路傳過來，然後丟掉大部分。搭配 `@>` 和一個 GIN 索引，過濾這件事直接在資料庫裡完成：

```sql
CREATE INDEX idx_user_preferences ON users USING GIN (preferences);
```

**核心思想**：能推回資料庫做的過濾，就不要留在應用程式碼裡做——尤其是資料量夠大的時候，這個差異不是「快一點」，而是數量級的差異。

#### 📊 實際效果

在一張有 80 萬筆使用者資料的表上，一條原本要花 2.3 秒（循序掃描 + 網路傳輸 + 應用層過濾）的查詢，改用 GIN 索引掃描後降到 12ms。作者是在 Ihsan 問「為什麼使用者分眾（segmentation）這支 API 這麼慢」之後，終於跑了一次 `EXPLAIN ANALYZE`，才知道這組數字的。

另一個值得認識的是 `jsonb_to_recordset`——它可以把一個 JSON 陣列直接展開成一列一列的資料：

```sql
SELECT name, score
FROM jsonb_to_recordset('[
    {"name": "Alice", "score": 95},
    {"name": "Bob",   "score": 82}
]'::jsonb) AS t(name text, score int)
WHERE score > 90;
```

作者用這個技巧，把分析管線（analytics pipeline）裡三個各自獨立的資料轉換函式，換成了單一查詢——維護的程式碼變少、往返次數變少，而且邏輯離它操作的資料更近了。

---

### 4. Generated Columns — 一個讓我稽核了兩天的同步 Bug

這個 bug 隱蔽到在 production 裡活了好幾個月才被抓到。

系統裡有一張 `products` 表，有一個 `price` 欄位，還有一個固定 11% 稅率的 `price_with_tax` 欄位。理論上，應用程式碼要負責在新增與更新時計算稅額欄位。

問題是，「理論上」不代表「每次都」。有一支直接更新價格的批次匯入腳本；有一支寫得很趕、後來被遺忘的內部管理端點；還有一次原本跑對的 migration，被複製、修改，又再跑了一次，這次跑錯了。

等到團隊發現時，大約 3% 的商品資料列，`price_with_tax` 已經跟 `price` 對不上了。兩天的稽核與修正。事後真正的解法，是一個 Generated Column：

```sql
ALTER TABLE products
ADD COLUMN price_with_tax NUMERIC
GENERATED ALWAYS AS (price * 1.11) STORED;
```

Generated Column 的值，是從 Schema 裡定義一次的公式算出來的。Postgres 會在每一次新增與更新時自動維護它——不管是哪個 client、哪一條程式碼路徑觸發的。你不可能把它設成錯的值，也不可能忘記更新它。

```sql
-- 這行會失敗——這是刻意設計的行為
UPDATE products SET price_with_tax = 99.99 WHERE id = 1;
-- ERROR: column "price_with_tax" can only be updated to DEFAULT

-- 這行沒問題，price_with_tax 會自動跟著重新算好
UPDATE products SET price = 89.99 WHERE id = 1;
```

**核心思想**：與其靠應用程式碼「記得」去維護一個衍生欄位，不如讓資料庫用 Schema 定義保證它永遠正確——這是「讓非法狀態無法被表達」這個原則，套用在資料表設計上的版本。

同樣的手法用在全文檢索向量上，會變得更有價值：

```sql
ALTER TABLE articles
ADD COLUMN search_vector tsvector
GENERATED ALWAYS AS (
    to_tsvector('english',
        coalesce(title, '') || ' ' || coalesce(body, ''))
) STORED;

CREATE INDEX idx_articles_fts ON articles USING GIN (search_vector);

-- 全文檢索，永遠是最新資料，不需要額外的同步流程
SELECT title FROM articles
WHERE search_vector @@ to_tsquery('english', 'postgresql & performance');
```

團隊原本為了文章搜尋另外養了一套 Elasticsearch，而它跟資料庫大約有 2% 的時間對不上——而且往往就發生在使用者最容易注意到的那些時刻。團隊後來把它整套除役：少維運一個服務，搜尋結果也再也不會過期。

---

### 5. Row-Level Security — 一次差點釀成資安事故的疏漏

這一段我會講得特別小心，因為細節很重要。

系統是一個多租戶（Multi-tenant）SaaS，所有客戶的資料都放在同一個資料庫裡，靠 `tenant_id` 區分。規則很簡單：任何會碰到客戶資料的查詢，都必須加上 `WHERE tenant_id = $1`。而這條規則，完全只靠「開發者的自律」來把關。

十二支 API 端點裡，十一支都寫對了。第十二支——是在一個大家都趕著出貨的 Sprint 裡，臨時加上去的內部報表端點——沒有寫對。

團隊在上線前的資安審查裡發現了這個漏洞。但那是「用人工讀程式碼」抓出來的，沒有任何系統性的保證機制。

Row-Level Security 把這份保證，從「開發者的自律」搬進了資料庫層：

```sql
ALTER TABLE documents ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON documents
    USING (tenant_id = current_setting('app.tenant_id')::int);
```

在每個請求開始時，設定當下的租戶（tenant）上下文：

```csharp
public async Task WithTenantContextAsync(
    NpgsqlConnection connection, int tenantId,
    Func<NpgsqlTransaction, Task> action, CancellationToken cancellationToken)
{
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    // 用 set_config(..., true) 而不是 SET LOCAL app.tenant_id = $1，
    // 因為 SET 是 utility statement，多數走 extended query protocol 的驅動程式
    // （包含 Npgsql）並不支援對它綁定參數
    await using (var setTenantCmd = new NpgsqlCommand(
        "SELECT set_config('app.tenant_id', $1, true)", connection, transaction))
    {
        setTenantCmd.Parameters.AddWithValue(tenantId.ToString());
        await setTenantCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    try
    {
        await action(transaction);
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

現在，在這個 transaction 裡執行的任何查詢——不管應用程式碼寫了什麼、不管是哪個開發者寫的、不管他們有沒有記得加上 `WHERE` 條件——都會自動被過濾成只回傳這個租戶的資料。資料庫會在回傳結果之前，強制把關保護。

**值得先知道的除錯眉角**：一個回傳零筆資料的查詢，有可能是因為 RLS 擋掉了，而不是因為資料真的不存在。開發期間，用 superuser 連線（預設會繞過 RLS）或暫時停用 Policy 來測試，有時候是必要的手段。這件事最好在你半夜 11 點踩到之前就先知道。

---

### 結尾：Ihsan 每次架構討論前都會問的問題

回頭看過去這十八個月，模式很清楚：每一次，作者手上都有一個「能動」的解法——一個輪詢迴圈、一個 Redis 鎖、應用層的 JSON 過濾、手動維護的計算欄位、靠慣例把關的 `WHERE` 條件。而每一次，Postgres 都原生內建了一個更可靠、更容易維運，或兩者兼具的能力。

這不是在反對專門工具的存在。Kafka 能處理的事件串流規模，是 LISTEN/NOTIFY 會直接撐爆的量級；Elasticsearch 的能力，也不是 Generated Column 做出來的全文檢索索引能比得上的；面對複雜的協調情境，Redis 的功能也比 Advisory Lock 豐富得多。這些都是真實的取捨，不是藉口。

但「Good enough（夠用就好）」是一個真實存在的工程類別，而作者長期以來系統性地低估了 Postgres 的「夠用」到底能延伸到多遠。每多一項基礎設施——每一個要部署、監控、升級、除錯的額外服務——都是有成本的。如果 Postgres 就能把事情做好，更簡單的系統通常才是對的選擇。

回頭看，真正讓作者在意的，不是「做了一個經過深思熟慮的取捨」，而是他當初根本不知道 LISTEN/NOTIFY 存在、從沒聽過 Advisory Lock、以為 JSONB 就只是拿來存放任意 blob 的容器。他只是預設走向自己已經熟悉的模式，卻沒意識到自己每天在用的工具裡，其實藏著更好的選項。

> 這是一種和「選錯技術」完全不同的錯誤。這是「不知道自己不知道」。

Ihsan 到現在，在每一次討論後端架構決策之前，都還是會問同一句話：

> 「你確認過 Postgres 是不是已經能做到這件事了嗎？」

有點煩。但她幾乎每次都是對的。

---

## 🎯 資深專業 DBA 評論

### 整體評價

這篇文章最聰明的地方，在於它沒有寫成「5 個你不知道的 Postgres 冷知識」這種清單文，而是把每一個功能都釘在一次真實的生產事故上——時間點、數字、事後的取捨，全部都在。這種寫法比條列式的 feature dump 更有說服力，因為它示範的不是「這個功能存在」，而是「不知道這個功能存在，實際上讓你付出了什麼代價」。文中提到的五個能力（LISTEN/NOTIFY、Advisory Lock、JSONB 運算子、Generated Column、Row-Level Security）也選得很精準：它們都是「Postgres 內建、但多數團隊會下意識繞道用其他工具解決」的典型案例。

---

### 值得肯定的地方

**用具體數字取代模糊的「效能有變好」，是這篇文章最扎實的部分。** 340ms 到 18ms、2.3 秒到 12ms，這種對比讓讀者能自己判斷「這個功能值不值得學」，而不是被單純的形容詞說服。這也是為什麼用 `EXPLAIN ANALYZE` 驗證假設（而不是憑感覺優化）值得被特別記下來——文中作者自己也是被 Ihsan 逼著跑了一次才發現真相。

**Row-Level Security 那一段是全文份量最重的案例。** 把授權規則從「開發者自律」搬進資料庫層，是一個真正屬於資安層級的架構升級，不只是效能優化。十二支端點裡有一支忘了加 `WHERE tenant_id`，這種情境在任何多租戶系統裡都可能發生，而且往往是「趕出貨的那個 Sprint」最容易出包——RLS 把「人會不會記得」這個變數，直接從風險方程式裡拿掉。

**Generated Column 解決的 Bug 類型，其實是很多團隊的通病。** 「衍生欄位需要應用程式碼手動同步」幾乎必然會隨著程式碼路徑變多（批次腳本、管理後台、migration）而失守。把這個責任交還給 Schema 本身，是把「Make Illegal States Unrepresentable」這個常見於強型別語言的設計哲學，搬到了資料庫層級落地。

---

### 可以更深入的地方

**LISTEN/NOTIFY 有兩個文章沒講清楚的地雷。** 第一，`NOTIFY` 的 payload 長度上限是 8000 bytes，超過會直接報錯，所以不能拿它當一般的訊息內容載體，只適合放 ID 或極簡的中繼資料，實際資料還是得回頭查表。第二，「訊息遺失」的風險不只發生在「NOTIFY 觸發時沒有 listener 連著」，連線斷線重連的空窗期一樣會漏接通知。真正要用在生產環境，通常需要搭配一支「重新連線後，補撈一次可能漏接資料」的 reconciliation 查詢，文章對這個 fallback 機制完全沒有提到，直接照搬文中架構上線是有風險的。

**Advisory Lock 其實有兩種層級，文章只示範了一種。** `pg_advisory_lock` / `pg_try_advisory_lock` 是 Session 層級，需要顯式解鎖或等連線關閉；`pg_advisory_xact_lock` 則是 Transaction 層級，交易結束（commit 或 rollback）就自動釋放，通常更安全、更少見到忘記解鎖的情境。更重要的是一個很多人會踩到的坑：**如果應用程式用連線池（connection pool），Session 層級的鎖是綁在「實際的資料庫連線」上，而不是「一次邏輯上的請求」**。連線被連線池回收再利用時，鎖有可能沒有照預期釋放，或被完全不相關的下一個請求誤用。想用 Advisory Lock，得先確認你的連線管理策略跟它的生命週期假設是相容的。

**Generated Column 的限制文章完全沒提。** `STORED` 的 Generated Column 不能參照同一張表以外的其他資料表，也不能是 Volatile（不確定性）的運算式；既有資料要補上新 Generated Column 時，需要重跑一次 migration 才會回填，不是加了欄位定義就自動生效。另外，`STORED` 是「寫入當下就算好存起來」，在寫入頻繁的資料表上，等於是多了一次計算與一份額外儲存空間的寫入放大（write amplification），高頻寫入的場景要先評估這個代價。

**RLS 的效能代價與例外情境，是全文最大的空白。** Policy 裡的條件式會被併入每一條查詢的執行計畫，如果 Policy 寫得不夠精簡（例如藏了子查詢或呼叫了昂貴的 function），會讓「每一條」碰到這張表的查詢都變慢，而且不一定能從 `EXPLAIN` 一眼看出來是 Policy 造成的，上線前最好用 `EXPLAIN (ANALYZE, COSTS)` 驗證過。另外，RLS 預設不會套用在資料表的 Owner 與 Superuser 身上（除非額外下 `ALTER TABLE ... FORCE ROW LEVEL SECURITY`），這正是文章提到「用 superuser 連線繞過 RLS 來除錯」的原因——但這同時也是一個容易被誤用的後門，維運上要嚴格控管誰持有 superuser 連線字串。最後，文中 C# 範例把 Go 原文的 `SET LOCAL app.tenant_id = $1` 改成了 `set_config('app.tenant_id', $1, true)`，這不是隨意的風格選擇：`SET` 是 utility statement，在走 extended query protocol 的驅動程式（包含 Npgsql）底下通常無法綁定參數，`set_config()` 才是官方建議、真正能參數化的寫法。

---

### 總結評分

| 面向 | 評分 |
|------|------|
| 實用性 | ⭐⭐⭐⭐⭐ |
| 深度 | ⭐⭐⭐☆☆ |
| 新穎性 | ⭐⭐⭐⭐☆ |
| 適合對象 | 中階工程師、後端 / 全端工程師、以 Postgres 為主要資料庫的團隊 |

**這是一篇用真實事故包裝的「Postgres 進階功能導覽」**，勝在故事說得好、數字給得實在，缺點是每個功能的「地雷與限制」都點到為止。如果決定要在生產環境導入文中任何一項技術，建議搭配官方文件把邊界條件（payload 上限、鎖的層級、Generated Column 的限制、RLS 的效能與例外）再讀一輪。延伸閱讀：

- *PostgreSQL Documentation* — 官方文件對 LISTEN/NOTIFY、Advisory Locks、RLS 都有專章，是驗證任何「聽說 Postgres 可以」說法的第一手來源
- *Designing Data-Intensive Applications* — Martin Kleppmann（理解 Notify/Listen 這類機制在分散式系統裡的定位與取捨）
- *The Art of PostgreSQL* — Dimitri Fontaine（更系統性地涵蓋 JSONB、Generated Column 等進階 Schema 設計技巧）
- *PostgreSQL 官方 Wiki - Don't Do This* — 收錄大量「看似合理但其實有坑」的反模式，適合搭配本文的「地雷」段落閱讀
