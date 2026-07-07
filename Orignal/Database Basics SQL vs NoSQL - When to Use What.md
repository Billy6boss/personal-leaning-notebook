#Database Basics: SQL vs NoSQL — When to Use What

Every backend developer faces this moment.

You’re designing a new system. You need to store data. And suddenly you’re drowning in options:

PostgreSQL. MongoDB. MySQL. Redis. DynamoDB. Cassandra. CockroachDB.

Each has fans swearing it’s the only real choice. Each has horror stories from people who picked wrong.

Here’s what nobody tells you early enough: the database you choose shapes everything. Your data model. Your queries. Your scaling strategy. Your 3 AM debugging sessions.

I’ve migrated databases mid-project. It’s painful. It’s expensive. It’s avoidable — if you understand the trade-offs upfront.

This guide breaks down SQL vs NoSQL in a way that actually helps you decide. No academic theory. No “it depends” hand-waving. Real patterns, real use cases, real guidance.

Let’s get into it.

The 60-Second Overview
Before we dive deep:

Press enter or click to view image in full size

Now let’s understand what this actually means.

What Problem Are We Solving?
A database does three things:

Store data — persist information reliably
Retrieve data — find what you need, fast
Maintain integrity — ensure data stays consistent and valid
SQL and NoSQL databases approach these differently. Neither is “better” — they’re optimized for different scenarios.

SQL Databases: The Structured Powerhouse
SQL (Structured Query Language) databases have been around since the 1970s. They’re called “relational” because they store data in tables with relationships between them.

The Philosophy
"Define your structure first. Enforce it always. Query anything."
You design tables. You define relationships. The database enforces rules. In return, you get powerful, flexible queries and guaranteed data integrity.

What SQL Looks Like
Tables:

Press enter or click to view image in full size

Relationships via foreign keys:

-- posts.user_id references users.id
-- This enforces: every post MUST belong to a valid user
SQL in Action
Creating tables:

CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    content TEXT,
    user_id INTEGER NOT NULL REFERENCES users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Index for faster lookups
CREATE INDEX idx_posts_user_id ON posts(user_id);
Querying data:

-- Get a user
SELECT * FROM users WHERE id = 1;

-- Get user with their posts (JOIN)
SELECT users.username, posts.title, posts.created_at
FROM users
JOIN posts ON posts.user_id = users.id
WHERE users.id = 1;

-- Count posts per user
SELECT users.username, COUNT(posts.id) as post_count
FROM users
LEFT JOIN posts ON posts.user_id = users.id
GROUP BY users.id
ORDER BY post_count DESC;

-- Complex query: users who posted in the last 7 days
SELECT DISTINCT users.*
FROM users
JOIN posts ON posts.user_id = users.id
WHERE posts.created_at > NOW() - INTERVAL '7 days';
The power of SQL: You can ask almost any question about your data, even questions you didn’t anticipate when designing the schema.

ACID: The Reliability Guarantee
SQL databases guarantee ACID properties:

Atomicity — Transactions are all-or-nothing
Consistency — Data always follows your rules
Isolation — Concurrent transactions don’t interfere
Durability — Committed data survives crashes
Example — Bank transfer:

BEGIN TRANSACTION;

UPDATE accounts SET balance = balance - 100 WHERE id = 1;
UPDATE accounts SET balance = balance + 100 WHERE id = 2;

COMMIT;
-- Either BOTH happen, or NEITHER happens. Never half-done.
This is why banks use SQL databases. Losing money to bugs is not an option.

Popular SQL Databases
Press enter or click to view image in full size

My default recommendation: PostgreSQL. It handles 90% of use cases excellently.

The Good
✅ Powerful queries — JOINs, aggregations, subqueries, window functions

✅ Data integrity — constraints, foreign keys, transactions

✅ Mature ecosystem — 50+ years of tooling, knowledge, optimization

✅ ACID guarantees — reliable, predictable behavior

✅ Flexible querying — ask questions you didn’t plan for

The Bad
❌ Rigid schema — changing structure requires migrations

❌ Vertical scaling — limited by single server capacity

❌ Impedance mismatch — objects ↔ tables mapping can be awkward

❌ Complexity — normalization, JOINs, indexes require expertise

❌ Not great for — rapidly changing schemas, massive write throughput

NoSQL Databases: The Flexible Family
NoSQL (“Not Only SQL”) emerged in the 2000s to handle problems SQL struggled with: massive scale, flexible schemas, and specific data patterns.

But here’s the thing: NoSQL isn’t one thing. It’s a family of four different database types.

The Four Types of NoSQL
Press enter or click to view image in full size

Let’s break each down.

Document Databases (MongoDB, CouchDB)
Store data as JSON-like documents. Each document can have a different structure.

The Philosophy
"Store data the way your application uses it."
Instead of splitting data across tables and JOINing, you store related data together in one document.

What Document DBs Look Like
A user document in MongoDB:

{
  "_id": ObjectId("507f1f77bcf86cd799439011"),
  "username": "john_doe",
  "email": "john@example.com",
  "created_at": ISODate("2024-01-15T10:30:00Z"),
  "profile": {
    "bio": "Backend engineer",
    "location": "San Francisco",
    "website": "https://johndoe.dev"
  },
  "posts": [
    {
      "title": "My First Post",
      "content": "Hello world...",
      "created_at": ISODate("2024-02-01T14:00:00Z"),
      "tags": ["intro", "personal"]
    },
    {
      "title": "Learning MongoDB",
      "content": "Documents are great...",
      "created_at": ISODate("2024-02-05T09:30:00Z"),
      "tags": ["tech", "database"]
    }
  ],
  "settings": {
    "notifications": true,
    "theme": "dark",
    "language": "en"
  }
}
Notice: everything about this user is in one document. No JOINs needed.

MongoDB in Action
Creating and querying:

// Insert a document
db.users.insertOne({
  username: "john_doe",
  email: "john@example.com",
  profile: { bio: "Backend engineer" },
  posts: []
});

// Find a user
db.users.findOne({ username: "john_doe" });

// Find users in San Francisco
db.users.find({ "profile.location": "San Francisco" });

// Add a post to a user (update nested array)
db.users.updateOne(
  { username: "john_doe" },
  { $push: { posts: { title: "New Post", created_at: new Date() } } }
);

// Find users with more than 5 posts
db.users.find({ $expr: { $gt: [{ $size: "$posts" }, 5] } });

// Aggregation pipeline (like SQL GROUP BY)
db.users.aggregate([
  { $unwind: "$posts" },
  { $group: { _id: "$username", post_count: { $sum: 1 } } },
  { $sort: { post_count: -1 } }
]);
When Documents Shine
Variable schemas — each user can have different fields
Hierarchical data — nested objects are natural
Read-heavy with known access patterns — fetch everything in one query
Rapid prototyping — no migrations, just store what you need
When Documents Struggle
Many-to-many relationships — gets messy fast
Cross-document queries — no JOINs (or limited support)
Data integrity — no foreign keys to enforce relationships
Unknown query patterns — hard to optimize what you can’t predict
Key-Value Stores (Redis, DynamoDB)
The simplest model: a key maps to a value. That’s it.

The Philosophy
"Blazing fast lookups by key. Nothing else matters."
What Key-Value Looks Like
Press enter or click to view image in full size

Redis in Action
import redis

r = redis.Redis(host='localhost', port=6379, db=0)

# Basic operations
r.set("user:42:name", "john_doe")
r.get("user:42:name")  # b"john_doe"

# Expiring keys (great for sessions/cache)
r.setex("session:abc123", 3600, "user_42")  # Expires in 1 hour

# Atomic counters
r.incr("page:home:views")  # Thread-safe increment

# Lists (message queues)
r.lpush("queue:emails", json.dumps({"to": "john@example.com"}))
r.rpop("queue:emails")  # Process from the other end

# Sets (unique collections)
r.sadd("user:42:followers", 101, 102, 103)
r.sismember("user:42:followers", 101)  # True

# Sorted sets (leaderboards)
r.zadd("leaderboard", {"alice": 100, "bob": 85, "charlie": 92})
r.zrevrange("leaderboard", 0, 2)  # Top 3: ['alice', 'charlie', 'bob']

# Hash (object-like)
r.hset("user:42", mapping={"name": "john", "email": "john@example.com"})
r.hgetall("user:42")  # {b'name': b'john', b'email': b'john@example.com'}
When Key-Value Shines
Caching — store computed results, reduce database load
Session storage — fast reads, automatic expiration
Rate limiting — atomic counters with TTL
Leaderboards/rankings — sorted sets are perfect
Real-time data — pub/sub, message queues
When Key-Value Struggles
Complex queries — can only lookup by key
Relationships — no concept of linking data
Large datasets — everything in memory (Redis) gets expensive
Primary data store — usually a complement, not replacement
Wide-Column Stores (Cassandra, HBase)
Designed for massive scale across many servers. Think: billions of rows, petabytes of data.

The Philosophy
"Write everything, read by known patterns, scale infinitely."
What Wide-Column Looks Like
Press enter or click to view image in full size

Each row can have different columns. Columns are grouped into families.

Cassandra in Action
-- Create a table (Cassandra Query Language - CQL)
CREATE TABLE posts_by_user (
    user_id UUID,
    post_id TIMEUUID,
    title TEXT,
    content TEXT,
    PRIMARY KEY (user_id, post_id)
) WITH CLUSTERING ORDER BY (post_id DESC);

-- Insert data
INSERT INTO posts_by_user (user_id, post_id, title, content)
VALUES (uuid(), now(), 'My First Post', 'Hello world...');

-- Query by partition key (fast!)
SELECT * FROM posts_by_user WHERE user_id = ?;

-- Query with clustering key (also fast)
SELECT * FROM posts_by_user 
WHERE user_id = ? 
AND post_id > minTimeuuid('2024-01-01');

-- This is SLOW or FORBIDDEN (no partition key):
-- SELECT * FROM posts_by_user WHERE title = 'My First Post';
The catch: You must query by the partition key. Cassandra is optimized for specific access patterns, not ad-hoc queries.

When Wide-Column Shines
Massive write throughput — millions of writes per second
Time-series data — IoT sensors, logs, metrics
Geographic distribution — built for multi-datacenter
Linear scalability — add nodes, get more capacity
When Wide-Column Struggles
Ad-hoc queries — must design tables around query patterns
Transactions — limited support
Small scale — overkill for most applications
Complexity — requires deep expertise to operate well
Graph Databases (Neo4j, Neptune)
Store data as nodes and relationships. Perfect for highly connected data.

The Philosophy
"Relationships are first-class citizens, not afterthoughts."
What Graph DBs Look Like
Press enter or click to view image in full size

Nodes (circles) have properties. Edges (arrows) represent relationships.

Neo4j in Action (Cypher Query Language)
// Create nodes
CREATE (alice:User {name: 'Alice', email: 'alice@example.com'})
CREATE (bob:User {name: 'Bob', email: 'bob@example.com'})
CREATE (post:Post {title: 'Hello World', content: '...'})

// Create relationships
CREATE (alice)-[:FOLLOWS]->(bob)
CREATE (alice)-[:WROTE]->(post)
CREATE (bob)-[:LIKES]->(post)

// Find who Alice follows
MATCH (alice:User {name: 'Alice'})-[:FOLLOWS]->(friend)
RETURN friend.name

// Find friends of friends (2 hops)
MATCH (alice:User {name: 'Alice'})-[:FOLLOWS*2]->(fof)
RETURN DISTINCT fof.name

// Find mutual followers
MATCH (alice:User {name: 'Alice'})-[:FOLLOWS]->(mutual)<-[:FOLLOWS]-(bob:User {name: 'Bob'})
RETURN mutual.name

// Shortest path between two users
MATCH path = shortestPath(
  (alice:User {name: 'Alice'})-[:FOLLOWS*]-(bob:User {name: 'Bob'})
)
RETURN path

// Recommend posts liked by people Alice follows
MATCH (alice:User {name: 'Alice'})-[:FOLLOWS]->(friend)-[:LIKES]->(post)
WHERE NOT (alice)-[:LIKES]->(post)
RETURN post.title, COUNT(friend) as friend_likes
ORDER BY friend_likes DESC
When Graph DBs Shine
Social networks — friends, followers, connections
Recommendation engines — “people who liked X also liked Y”
Fraud detection — finding suspicious patterns in transactions
Knowledge graphs — connected information (Wikipedia, Google Knowledge Graph)
Network/IT infrastructure — dependencies, impact analysis
When Graph DBs Struggle
Simple CRUD — overkill if you don’t need relationship queries
Analytics/aggregations — not designed for “count all users”
Massive scale — horizontal scaling is harder than Cassandra
Learning curve — Cypher/graph thinking takes time
The Decision Framework
Let’s make this practical. Answer these questions:

1. How structured is your data?
Highly structured, known upfront → SQL
Variable, evolving, nested → Document (MongoDB)
Simple key-based access → Key-Value (Redis)
Relationship-heavy → Graph (Neo4j)
2. What are your query patterns?
Complex, ad-hoc queries → SQL
Fetch by ID/known patterns → NoSQL (Document, Key-Value)
Traversing connections → Graph
Time-series, append-heavy → Wide-Column (Cassandra)
3. How important is data integrity?
Critical (money, inventory) → SQL with ACID
Eventually consistent is OK → NoSQL
Both needed → SQL primary + NoSQL cache
4. What’s your scale?
< 1TB, < 10K requests/sec → SQL handles this fine
Massive writes, petabytes → Wide-Column
Massive reads, caching → Key-Value
Moderate scale, flexible schema → Document
5. What’s your team’s expertise?
SQL/relational experience → Start with SQL
Document/JSON-native thinking → MongoDB might feel natural
New to databases → PostgreSQL (best docs, most transferable skills)
Common Combinations
Most production systems use multiple databases:

The Classic Web App
Press enter or click to view image in full size

The Social Platform
Press enter or click to view image in full size

The IoT / Analytics Platform
Press enter or click to view image in full size

Quick Reference: Pick Your Database
Press enter or click to view image in full size

My Honest Recommendations
If you’re starting a new project:
Default to PostgreSQL. Seriously.

It handles relational data beautifully, has excellent JSON support (almost like a document DB), scales further than most projects need, and the skills transfer everywhere.

Add Redis for caching when you need it. That combo covers 80% of applications.

If you’re building something specific:
Social app with complex relationships → PostgreSQL + Neo4j
Real-time analytics/IoT → PostgreSQL + Cassandra/TimescaleDB
Content management with flexible schemas → MongoDB (but PostgreSQL’s JSONB is often enough)
High-traffic caching layer → Redis (always Redis)
If you’re unsure:
Start with PostgreSQL. You can always add specialized databases later. But migrating away from a bad initial choice? That’s painful.

The Mistakes I’ve Made (So You Don’t Have To)
Mistake 1: Choosing MongoDB because “NoSQL is faster”
It’s not inherently faster. I picked MongoDB for a project with lots of relationships. Ended up doing application-level JOINs. Should have used PostgreSQL.

Mistake 2: Not using Redis early enough
I optimized database queries for weeks. Added Redis caching in one day — 10x improvement. Cache your hot paths.

Mistake 3: Over-engineering with multiple databases
A startup I worked at used PostgreSQL, MongoDB, Elasticsearch, AND Redis from day one. We spent more time syncing data between them than building features. Start simple.

Mistake 4: Ignoring indexing
Slow queries aren’t a database problem — they’re an indexing problem. Learn to use EXPLAIN ANALYZE. Index your WHERE clauses.

The Bottom Line
There’s no perfect database. There’s only the right fit for your specific needs.

SQL when you need reliability, complex queries, and data integrity
Document when schemas vary and you fetch whole objects
Key-Value when you need speed for simple lookups
Wide-Column when you’re at massive scale with known patterns
Graph when relationships are your primary concern
Most of the time? PostgreSQL + Redis will take you further than you think.

Start there. Add complexity only when you have evidence you need it.