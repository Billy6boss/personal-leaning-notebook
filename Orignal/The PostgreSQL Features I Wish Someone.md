# The PostgreSQL Features I Wish Someone Had Shown Me Three Years Ago

It was 9:14 AM on a Tuesday when our biggest client called. Not emailed. Called.

Their morning dashboard was timing out had been for twenty minutes. Our status page said everything was green, which made it worse because the problem was invisible to us and very visible to them.

I found it fast: a polling loop I’d written six months earlier, checking a table every five seconds for new tasks. Under morning load, with fifty users hitting the dashboard simultaneously, the database was drowning in concurrent reads. I patched it in forty minutes better indexing, tighter query and load time dropped from 800ms to something tolerable. I called it a win.

Three weeks later, I was in a code review with Ihsan, a senior engineer who’d moved to our team from a database consultancy. She looked at my polling loop for about four seconds.

“Why aren’t you using LISTEN/NOTIFY?” she asked.

I had no idea what she was talking about.

That moment kicked off a month of conversations where Ihsan, one piece at a time, showed me what I’d been missing. I’d been writing PostgreSQL queries for three years. I thought I knew the tool. What I actually knew was a thin slice of it.

LISTEN / NOTIFY: the event system hiding in plain sight
The polling loop I’d built was the obvious solution check the table, pick up pending rows, process them. Every backend developer has written one. It also does unnecessary work on every iteration when nothing has changed, and generates competing reads that pile up under load.

What Ihsanshowed me was that Postgres has had a native pub/sub mechanism since version 9.0.

-- Any session subscribes to a named channel
LISTEN task_created;

-- Any other session broadcasts to it
NOTIFY task_created, '{"task_id": 8821, "priority": "high"}';
The listener receives the payload in milliseconds. No polling interval, no thundering herd, no reads when nothing has changed. Pair it with a trigger and new tasks publish themselves:

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
In Go, with lib/pq:

func startTaskListener(connStr string) {
    listener := pq.NewListener(connStr, 10*time.Second, time.Minute,
        func(ev pq.ListenerEventType, err error) {
            if err != nil {
                log.Println("listener error:", err)
            }
        })

   if err := listener.Listen("task_created"); err != nil {
        log.Fatal(err)
    }
 for {
        select {
        case n := <-listener.Notify:
            if n != nil {
                processTaskNotification(n.Extra)
            }
        case <-time.After(90 * time.Second):
            listener.Ping()
        }
    }
}
Here’s what the architecture difference looks like in practice:

Press enter or click to view image in full size

[diagram: polling vs. LISTEN/NOTIFY — see image above]

I ran both implementations under the exact load profile that had caused the Tuesday incident: 50 concurrent users, 200 tasks created per hour. The polling implementation averaged 340ms response time with spikes to 1.1 seconds when tasks happened to be created right before a poll cycle. The LISTEN/NOTIFY implementation averaged 18ms, flat under the same load.

The dashboard query itself was unchanged. All the improvement came from removing the polling reads that had been competing with it.

Ihsan was characteristically understated about this. “Polling is just a loop that does unnecessary work most of the time,” she said, and moved on.

The honest limit: LISTEN/NOTIFY is not a durable message queue. Messages disappear if no listener is connected when NOTIFY fires. For guaranteed delivery or high-throughput event streaming, you still want Kafka or RabbitMQ. But for event-driven coordination within a system that’s already on Postgres, this removes an entire layer of infrastructure.

Advisory Locks: distributed coordination without a lock table
Two months after the dashboard incident, we scaled the task processor to run on three instances. Which meant three instances were now picking up the same tasks and writing conflicting results.

The quick fix was a Redis lock SETNX with a 60-second expiry and a heartbeat process. It worked, but it added Redis as a hard dependency for a service that otherwise didn’t need it. We’d been dealing with “stale lock” incidents for months: a process would crash, leave a Redis lock behind, and the next run would hang until the expiry elapsed.

I mentioned this to Ihsan. She asked if I’d looked at advisory locks.

PostgreSQL advisory locks are application-defined locks stored entirely in Postgres. You request a lock identified by any integer, and the database coordinates access across all connected sessions.

-- Non-blocking: returns true if acquired, false if someone else holds it
SELECT pg_try_advisory_lock(12345);

-- Release explicitly
SELECT pg_advisory_unlock(12345);
Session-level advisory locks release automatically when the connection closes. If a process crashes mid-job, the lock disappears with it no stale entries, no expiry races, no heartbeat process.

func runExclusiveJob(db *sql.DB, jobName string) error {
    var lockID int64
    if err := db.QueryRow(
        "SELECT hashtext($1)::bigint", jobName,
    ).Scan(&lockID); err != nil {
        return err
    }
var acquired bool
    if err := db.QueryRow(
        "SELECT pg_try_advisory_lock($1)", lockID,
    ).Scan(&acquired); err != nil {
        return err
    }
if !acquired {
        return nil // Another instance is running — expected, not an error
    }
    defer db.Exec("SELECT pg_advisory_unlock($1)", lockID)

 return processJob(db)
}
We removed Redis from that service’s dependency list entirely. The stale lock incidents stopped immediately not because we fixed them, but because the self-healing property of session-level locks made them structurally impossible.

The real trade-off: Advisory locks tie your distributed coordination to your database uptime. If Postgres is down, the lock mechanism is down too. For our use case, where the job required database access anyway, this was irrelevant. If Postgres is unavailable, the job can’t run regardless.

JSONB operators: the queries I was doing in application code
Around this same time, Ihsan casually mentioned that I should look at the JSONB containment operators. She said it the way someone mentions a keyboard shortcut you didn’t know existed not unkindly, just matter-of-fact.

Most developers who use JSONB know -> and ->> for field extraction. What's less known is the set of operators for filtering within JSON structures:

-- @> "contains": find rows where the JSON includes this exact subset
SELECT * FROM users
WHERE preferences @> '{"notifications": {"email": true}}';

-- ? "key exists"
SELECT * FROM products WHERE attributes ? 'color';

-- ?| "any of these keys exist"
SELECT * FROM products WHERE attributes ?| array['color', 'size'];

-- ?& "all of these keys exist"
SELECT * FROM products WHERE attributes ?& array['color', 'size'];

-- jsonb_set: update one nested field without replacing the whole document
UPDATE users
SET preferences = jsonb_set(preferences, '{notifications, email}', 'false')
WHERE id = 42;
The @> operator changes how you write queries. I had been pulling JSONB columns into application code and filtering there transferring all the data across the network, then discarding most of it. With @> and a GIN index, the filtering happens in the database:

CREATE INDEX idx_user_preferences ON users USING GIN (preferences);
On a table with 800,000 user rows, a query that had been taking 2.3 seconds (sequential scan, network transfer, application-layer filter) dropped to 12ms (GIN index scan in the database). I know those numbers because Ihsan asked why the user segmentation endpoint was slow and I finally ran EXPLAIN ANALYZE on it.

jsonb_to_recordset is another one worth knowing it unnests a JSON array directly into rows:

SELECT name, score
FROM jsonb_to_recordset('[
    {"name": "Alice", "score": 95},
    {"name": "Bob",   "score": 82}
]'::jsonb) AS t(name text, score int)
WHERE score > 90;
I replaced three separate data transformation functions in our analytics pipeline with single queries using this. Less code to maintain, fewer round trips, and the logic lives closer to the data it operates on.

Generated Columns: the sync bug that cost two days
The bug was subtle enough that it lived in production for months before we found it.

We had a products table with a price column and a price_with_tax column fixed 11% rate. Application code was responsible for computing the tax column on insert and update.

Except sometimes it wasn’t. There was a bulk import script that updated prices directly. An admin endpoint written quickly and forgotten. A migration that had run correctly once and been copied, modified, and run again incorrectly.

By the time we caught it, roughly 3% of product rows had a price_with_tax that didn't match the price. Two days of auditing and correction. The real fix, implemented afterward, was a generated column:

ALTER TABLE products
ADD COLUMN price_with_tax NUMERIC
GENERATED ALWAYS AS (price * 1.11) STORED;
A generated column’s value is computed from a formula defined once in the schema. Postgres maintains it automatically on every insert and update, from every client, across every code path. You cannot set it to a wrong value. You cannot forget to update it.

-- This fails — intentionally
UPDATE products SET price_with_tax = 99.99 WHERE id = 1;
-- ERROR: column "price_with_tax" can only be updated to DEFAULT

-- This works, price_with_tax updates automatically
UPDATE products SET price = 89.99 WHERE id = 1;
The same pattern works for full-text search vectors, and this is where it gets genuinely useful:

ALTER TABLE articles
ADD COLUMN search_vector tsvector
GENERATED ALWAYS AS (
    to_tsvector('english',
        coalesce(title, '') || ' ' || coalesce(body, ''))
) STORED;


CREATE INDEX idx_articles_fts ON articles USING GIN (search_vector);

-- Full-text search, always current, no separate process
SELECT title FROM articles
WHERE search_vector @@ to_tsquery('english', 'postgresql & performance');
We had been running a separate Elasticsearch instance for article search. It was out of sync with the database roughly 2% of the time always during the moments users noticed most. We decommissioned it. One fewer service to operate, and search results that are never stale.

Row-Level Security: the authorization bug that almost became a breach
This one I’ll be careful about, because the details matter.

Multi-tenant SaaS, all customer data in the same database, distinguished by tenant_id. The rule was simple: every query that touched customer data had to include WHERE tenant_id = $1. The rule was enforced entirely by developer discipline.

Eleven of twelve endpoints had it right. The twelfth an internal reporting endpoint added during a sprint where everyone was moving fast did not.

We caught it in a security review before it reached production. But we caught it manually, by reading code. We had no systematic guarantee.

Row-Level Security moves that guarantee into the database:

ALTER TABLE documents ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON documents
    USING (tenant_id = current_setting('app.tenant_id')::int);
Set the tenant context at the start of each request:

func withTenantContext(db *sql.DB, tenantID int, fn func(*sql.Tx) error) error {
    tx, err := db.Begin()
    if err != nil {
        return err
    }

    if _, err = tx.Exec(
        "SET LOCAL app.tenant_id = $1", tenantID,
    ); err != nil {
        tx.Rollback()
        return err
    }

if err := fn(tx); err != nil {
        tx.Rollback()
        return err
    }

 return tx.Commit()
}
Now any query run within that transaction regardless of what the application code does, regardless of which developer wrote it, regardless of whether they remembered the WHERE clause is automatically filtered to the correct tenant’s data. The database enforces it before returning results.

The debugging caveat worth knowing upfront: A query that returns zero rows might be failing because of RLS, not because the data doesn’t exist. During development, connecting as a superuser (which bypasses RLS by default) or temporarily disabling the policy for testing is sometimes necessary. Worth knowing before you hit it at 11 PM.

What Ihsan asks before every architecture decision
Looking back at the past eighteen months, the pattern is clear.

In every case, I had a working solution. A polling loop. A Redis lock. Application-layer JSON filtering. Manually-maintained computed columns. WHERE clauses enforced by convention. And in every case, Postgres had a native capability that was more reliable, simpler to operate, or both.

That’s not an argument against specialized tools. Kafka handles event streaming at a scale where LISTEN/NOTIFY would break. Elasticsearch has capabilities that generated column FTS indexes don’t come close to matching. Redis has a richer feature set than advisory locks for complex coordination. These are real trade-offs, not excuses.

But “good enough” is an actual engineering category, and I’d been systematically underestimating how far Postgres’s “good enough” extended. Each additional piece of infrastructure another service to deploy, monitor, upgrade, and debug carries a cost. If Postgres handles it well, the simpler system is usually right.

What bothers me, looking back, is that I wasn’t making a considered trade-off. I didn’t know LISTEN/NOTIFY existed. I’d never heard of advisory locks. I thought JSONB was basically for storing arbitrary blobs. I was defaulting to patterns I already knew without realizing there were better options in the tool I was already using.

That’s a different kind of mistake than choosing the wrong technology. It’s not knowing what you don’t know.

Ihsan still asks, before we discuss any backend architecture decision: “have you checked if Postgres already does this?”

It’s slightly annoying. She’s right almost every time.