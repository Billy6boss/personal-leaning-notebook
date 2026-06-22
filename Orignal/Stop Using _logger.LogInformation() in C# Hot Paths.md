# Stop Using _logger.LogInformation() in C# Hot Paths
How LoggerMessage source generators reduce allocations, improve performance, and keep your .NET logs clean.
By Hossein Kohzadi

Learn how to improve .NET logging performance using LoggerMessage source generators, with practical C# examples for hot paths, APIs, and background workers. Reduce allocations, avoid boxing, and keep structured logs clean and maintainable.

🔗Available for non-Medium members here. 🌐

Press enter or click to view image in full size

Photo by Kai Pilger on Unsplash
Press enter or click to view image in full size

Is This Article Helpful?
👉 Leave a clap if you enjoyed this article!
👉 Follow me on Medium for more .NET architecture and performance insights.
👉 Subscribe to never miss a post — turn on email notifications 🔔!

Follow Me on LinkedIn for C# and .NET insights!
Every .NET developer has written _logger.LogInformation(...) or _logger.LogDebug(...) thousands of times.

It feels harmless — until that log call lands inside a cache lookup, middleware pipeline, background worker, or high-frequency API path. In those hot paths, traditional ILogger extension methods can create hidden allocations, boxing, and unnecessary GC pressure—exactly the kind of overhead that shows up when your application is under load.

The fix is not to stop logging. The fix is to log smarter.

By using LoggerMessage source generators, you can move logging boilerplate to compile time and give yourself cleaner, faster, strongly typed logging code. Let’s explore why this matters and how to implement it.

The Hidden Cost of Traditional Logging
To understand the solution, we first need to look at the problem. Let’s take a standard caching service and look at a typical debug log:

The “Bad” (Traditional) Way
_logger.LogDebug(
    "Questionnaire cache L1 hit for OriginId={OriginId}, DataSourceId={DataSourceId}, CorrelationId={CorrelationId}",
    originId, dataSourceId, correlationId);
On the surface, this looks like standard structured logging. But under the hood, every single time this line executes, the .NET runtime does a lot of heavy lifting.

What Actually Gets Expensive?
The biggest issue is not the log text itself. The performance hit comes from:

Boxing of value types: If originId is a Guid or an int, it must be boxed into an object to fit the method signature. This creates a heap allocation.
Parameter array allocation: The parameters are bundled into a hidden object[] array. Another heap allocation.
Runtime Formatting Overhead: The logging pipeline still has to process the message template and arguments at runtime, which requires evaluating arguments and handling string templates on the fly.
Microsoft explicitly recommends avoiding these traditional extensions in high-performance scenarios. According to their documentation on compile-time logging source generation, moving this to compile time reduces temporary allocations and copies to the maximum extent possible. This is also enforced if you enable CA1848: Use the LoggerMessage delegates.

C# in Action: The Source Generator Solution
Introduced in .NET 6, compile-time logging source generators solve all of these problems by generating highly optimized C# code behind the scenes before your app even runs.

The “Clean” (LoggerMessage) Way
Instead of calling _logger directly, you define a partial method and decorate it with the [LoggerMessage] attribute.

public static partial class QuestionnaireCacheServiceLog
{
    [LoggerMessage(
        EventId = 3101, 
        Level = LogLevel.Debug, 
        Message = "Questionnaire cache L1 hit for OriginId={OriginId}, DataSourceId={DataSourceId}, CorrelationId={CorrelationId}")]
    public static partial void L1Hit(ILogger logger, Guid originId, string dataSourceId, string correlationId);
}

// Usage in your service:
QuestionnaireCacheServiceLog.L1Hit(_logger, originId, dataSourceId, correlationId);
Why is this vastly superior?
When you compile your code, .NET generates the underlying implementation for L1Hit.

Zero runtime parsing: The template is parsed once at compile-time.
Zero boxing: The generated code is strongly-typed to accept Guid and string. No object casting is required.
Zero-parameter arrays: It uses optimized internal structures to bypass object[] allocations entirely.
⚠️ Architect’s Note: Source-generated logging reduces logging overhead, but it does not protect you from expensive expressions passed as arguments. If you are calling methods, building objects, or doing expensive formatting before logging, guard the call with _logger.IsEnabled(LogLevel.Debug).

if (_logger.IsEnabled(LogLevel.Debug))
{
    QuestionnaireCacheServiceLog.L1Hit(
        _logger,
        originId,
        dataSourceId,
        BuildExpensiveCorrelationId()); // Guarded from executing if Debug is off
}
The Proof: Benchmarking the Difference
Don’t just take my word for it. Let’s look at example benchmark results from a representative high-frequency logging test.

Benchmark environment: .NET 8, Release mode, BenchmarkDotNet, console logger disabled, 1,000,000 calls, Debug level enabled.


Benchmarking the Difference
If you have ever added “just one harmless log line” inside a loop, cache lookup, or middleware pipeline, this is the kind of hidden cost worth checking before production.

Beyond Performance: 4 Developer Experience Wins
Performance isn’t the only reason to switch. LoggerMessage dramatically improves code maintainability.

1. Compile-Time Type Safety
With traditional logging, if you mismatch your parameters, you won’t find out until runtime.

// ❌ Semantic bug waiting to happen
// The log property says UserId, but the value is actually a username.
_logger.LogDebug("User {UserId} not found", userName);

// ✅ Compile-time protection through a strongly typed method
UserNotFound(_logger, userId);
2. Centralized Event IDs
Scattering new EventId(3101) throughout your classes makes it impossible to track duplicates or audit your telemetry. By grouping your [LoggerMessage] attributes in a dedicated static class, you create a central registry of all telemetry events in your domain.

3. Cleaner Business Logic
Your domain services shouldn’t be cluttered with multi-line string templates. Moving logs to a dedicated partial class keeps your primary methods focused strictly on business rules.

4. Highly Optimized Structured Logging
LoggerMessage automatically creates highly optimized structured logs. When hooked up to Serilog or pushed to Application Insights, your log payload is structured, consistent, and easier to query:

{
  "message": "Questionnaire cache L1 hit for OriginId=123...",
  "OriginId": "123e4567-e89b-12d3-a456-426614174000",
  "eventId": 3101
}
Should You Use This Everywhere?
While powerful, you don’t need to rewrite every single log in your massive monolithic application today. Here is the pragmatic breakdown of when to use it:

✅ WHEN TO USE LoggerMessage:

Hot Paths: Code that runs constantly (e.g., Cache Hits/Misses, HTTP Request interceptors).
High-Frequency Background Workers: Message consumers processing thousands of events per second.
Performance-Critical APIs: Endpoints with strict SLA requirements where GC pauses cause timeouts.
❌ WHEN TRADITIONAL LOGGING IS FINE:

Startup/Shutdown Sequences: Configuration logging in Program.cs.
Rare Exception Handling: An error log inside a global exception handler that rarely triggers.
Temporary Debugging: Quick throwaway logs used locally.
🧰 Tools & Patterns I Recommend
To get the most out of this pattern, pair it with the right observability stack:

Serilog: The gold standard for structured logging in .NET. It seamlessly ingests LoggerMessage outputs.
OpenTelemetry: Use structured logs alongside metrics and traces. If you are logging a cache hit just to count it, use a metric counter instead.
Scrutor: For clean dependency injection of your loggers and services without polluting your Program.cs.
Press enter or click to view image in full size

Is This Article Helpful?
👉 Leave a clap if you enjoyed this article!
👉 Follow me on Medium for more .NET architecture and performance insights.
👉 Subscribe to never miss a post — turn on email notifications 🔔!

Follow Me on LinkedIn for C# and .NET insights!
🏁 Conclusion
Refactoring traditional _logger.LogInformation calls into LoggerMessage source generators is one of the highest ROI refactors you can do in a high-traffic .NET application.

A log line inside a hot path is not just observability — it is part of your performance profile.

Let’s recap the wins:

✅ Reduced allocations and boxing on your hot paths.
✅ Significantly faster execution times.
✅ Compile-time type safety for log parameters.
✅ Centralized Event IDs for better maintainability.