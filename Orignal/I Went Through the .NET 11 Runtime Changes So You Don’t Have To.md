# I Went Through the .NET 11 Runtime Changes So You Don’t Have To
Runtime Async is one of those changes that makes you stop and look twice because it isn’t just another small optimization. It changes how the runtime itself participates in async execution, with improvements to things like stack traces, debugging, and execution overhead.

But that’s not the only interesting thing happening in .NET 11.

There are plenty of runtime changes in the release notes, some genuinely useful, some technically impressive, and some that are probably not worth spending five minutes thinking about.

So I went through them and picked out the ones I actually think are worth knowing.

Let’s start with the biggest one.

1. .NET 11 Is Changing How async Works
Let’s start with the change that immediately caught my attention.

Runtime Async.

For years, when you wrote an async method, the compiler did a lot of the heavy lifting. It transformed the method into a state machine that could suspend when it hit an await and later resume from the appropriate point.

That model works. It has worked for a very long time.

But .NET 11 introduces Runtime Async, which moves much of that responsibility into the runtime itself. Instead of relying entirely on compiler-generated async state machines, the runtime manages suspension and resumption of async methods. Microsoft describes this as a significant step toward replacing compiler-generated state machines with runtime-managed async execution.

And honestly, this is much more interesting than another “async is slightly faster now” optimization.

The stack trace problem
One of the most immediately visible improvements is the call stack.

If you’ve ever debugged complicated async code, you’ve probably seen stack traces containing things like AsyncMethodBuilderCore.Start<TStateMachine> and other compiler-generated infrastructure.

That’s not particularly useful when you’re trying to answer a simple question:

“How did my code actually get here?”

With Runtime Async enabled, those compiler-generated frames disappear from the live call stack.

For example, imagine this:

await OuterAsync();

static async Task OuterAsync()
{
    await Task.CompletedTask;
    await MiddleAsync();
}

static async Task MiddleAsync()
{
    await Task.CompletedTask;
    await InnerAsync();
}

static async Task InnerAsync()
{
    await Task.CompletedTask;
    Console.WriteLine(new StackTrace());
}
With the traditional async implementation, the live stack trace contains additional state-machine infrastructure between your methods.

With Runtime Async, the stack becomes much closer to what you’d actually expect:

InnerAsync
MiddleAsync
OuterAsync
Main
Microsoft’s example goes from 13 frames to 5 for this particular case. More importantly, the frames that remain are the methods you actually wrote.

That’s a much bigger deal for debugging and profiling than it might initially sound.

And there’s an important distinction here: this improvement is about live stack traces. Exception stack traces already receive cleanup through existing ExceptionDispatchInfo behavior, so Runtime Async isn't suddenly fixing exception stack traces that were completely broken before.

It isn’t just about stack traces
Runtime Async also brings improvements to how async execution itself is handled.

.NET 11 can reuse cached continuations, avoid saving locals that haven’t changed, and optimize common async paths. The JIT can also generate dedicated Runtime Async versions of certain task-returning methods rather than going through an additional thunk.

There are also improvements around ExecutionContext.

Normally, async continuations have to deal with ExecutionContext, which carries ambient state such as AsyncLocal<T> across asynchronous boundaries. .NET 11 can detect cases where there's actually nothing that needs to be restored and skip the unnecessary capture/restore work. This applies to Task, Task<T>, ValueTask, and ValueTask<T>.

So there are several pieces here, and that’s exactly why I don’t want to spend half this article pretending we can explain Runtime Async properly in a few paragraphs.

There’s a lot more going on here
Runtime Async is currently a preview feature, and the .NET runtime libraries themselves are already compiled with it enabled. It also supports ReadyToRun and NativeAOT scenarios, and Microsoft has continued adding JIT and async-specific optimizations around it.

I think Runtime Async deserves its own article.

There is a lot more to unpack around how the old state-machine model works, how runtime-managed suspension changes that model, what happens at an await, and what the JIT and runtime are actually doing behind the scenes.

For this article, the important takeaway is simpler:

.NET 11 isn’t just making async a little faster. It's changing where some of the machinery behind async execution lives.

And that is a pretty damn interesting runtime change.

2. The JIT Got Better at Proving Your Code Is Safe
Some of the best runtime improvements are the ones you never have to think about.

You write normal C#.

The JIT looks at it and goes, “I can prove that check isn’t necessary.”

.NET 11 has a few improvements exactly like that.

Bounds checks are getting smarter
Whenever you access an array or span, the runtime has to make sure your index is valid.

That’s necessary. Nobody wants IndexOutOfRangeException turning into memory corruption.

But sometimes the JIT can prove that an access is safe and eliminate the check entirely.

.NET 11 gets better at recognizing patterns such as:

for (int i = 0; i + 4 < span.Length; i++)
{
    DoSomething(span[i + 4]);
}
The JIT can now use the relationship between i + 4 < span.Length and the subsequent span[i + 4] access to prove that the bounds check isn't necessary.

It also gets better at eliminating redundant checks for index-from-end operations such as:

var last = values[^1];
These aren’t changes to the C# language. You don’t need to rewrite your code to take advantage of them.

They’re simply improvements in the JIT’s ability to reason about code that you’ve already written.

And this is exactly the kind of optimization I like seeing in a runtime.

You shouldn’t have to write weird code just to convince the JIT that an array access is safe.

Empty spans can now prove an access is valid
There’s another small example that’s particularly neat.

Consider:

if (!span.IsEmpty && span[0] == value)
{
    // ...
}
Once the JIT sees !span.IsEmpty, it knows the span contains at least one element.

That means the bounds check for span[0] is redundant.

.NET 11 can now make that connection and remove the unnecessary check.

It’s a tiny optimization, but it demonstrates something important about where the JIT is heading.

It’s not just looking at individual operations anymore. It’s getting better at carrying facts from one part of your code into another and using those facts to simplify the generated code.

Redundant conditions can disappear too
.NET 11 also improves redundant branch and test elimination.

Take this:

if (x > 0)
{
    if (x > 1)
    {
        DoSomething();
    }
}
Once execution reaches the inner condition, x > 1 already proves x > 0.

The outer check doesn’t need to remain part of the generated code.

The same idea applies to values produced by conditional expressions. If the JIT can prove what a value must be on each branch, it can sometimes eliminate the test that follows it entirely.

Again, none of this changes how you write the code.

And that’s the point.

These improvements are almost invisible to developers, but they’re exactly what I want from a JIT. The runtime gets better at understanding perfectly ordinary C# and quietly turns it into better machine code.

There are more JIT changes in .NET 11, including redundant checked-context removal and better folding of certain switch expressions. They’re interesting, but I wouldn’t pretend they’re all equally important.

The bigger story is that the JIT keeps getting better at proving what your code does, which means it can remove work that you never needed in the first place.

3. The JIT Is Getting Better at Using Modern CPUs
If you don’t write SIMD-heavy code, most of this section probably isn’t going to change your life.

And that’s fine.

But there are some genuinely interesting improvements here because .NET 11 isn’t just adding more hardware intrinsics. The JIT is also getting better at deciding when and how to use the hardware that’s already available.

Half conversions can finally use the hardware properly
On x64 CPUs that support F16C, .NET 11 can use the dedicated CPU instructions for converting between Half and float or double.

That’s important because these conversions no longer need to fall back to helper calls when the hardware can do the operation directly. F16C is available on most AVX2-capable x64 processors, so this isn’t some obscure feature you’re likely to find only on a laboratory machine.

If you’re doing numerical work involving half-precision values, that’s a pretty straightforward win.

If you’re building a CRUD API?

Yeah, I wouldn’t lose sleep over it.

The JIT’s SIMD cost model got an update
This one is more interesting to me.

The JIT has to make decisions about whether an optimization is actually worth doing. With SIMD code, that means it needs some idea of how expensive different operations are.

.NET 11 updates the x86/x64 SIMD cost model to better reflect modern SSE and AVX hardware rather than relying on older assumptions. That gives the JIT better information when making decisions around things like hoisting and common subexpression elimination.

This is the kind of improvement that doesn’t give you a new API to call.

You just give the JIT some code, and it has a better chance of making the right decision.

That’s a much more interesting runtime improvement than another method being added to some API surface.

DotProduct gets faster on AVX
.NET 11 also changes how certain vector dot products are lowered on AVX-capable hardware.

Instead of generating the vdpps/vdppd instructions, the JIT can use a sequence of multiply, permute, and add operations that Microsoft reports as consistently faster for these operations.

Again, this is highly workload-dependent.

If you’re doing numerical computing, image processing, machine learning, or another SIMD-heavy workload, this could matter.

If you’re writing an ASP.NET Core API that mostly moves JSON around, probably not.

And that’s okay.

Not every runtime optimization needs to benefit every application to be worthwhile.

Arm64 gets some love too
.NET 11 also improves IndexOfAnyAsciiSearcher on Arm64.

The relevant vectorized implementations of Count, IndexOf, and LastIndexOf no longer go through ExtractMostSignificantBits, and Microsoft reports 5% to 50% improvements in workloads where these operations are a significant part of the core loop.

That’s a much more concrete improvement than “we added some new intrinsics.”

It targets operations that real applications use, particularly when searching through text.

There are also new SVE and SVE2 capabilities, including additional SVE2 intrinsics and expanded instruction-set detection.

Those are useful additions for code that actually targets those CPUs, but I’m not going to spend three paragraphs explaining every new intrinsic.

You probably don’t need that.

The part I actually like
The interesting thing here isn’t any single intrinsic.

It’s that the runtime is continuing to get better at meeting the hardware where it is.

Modern CPUs have capabilities that simply didn’t exist when some of the older runtime assumptions were made. The JIT needs to understand those capabilities, understand their costs, and generate code that makes sensible use of them.

.NET 11 does more of that.

For most applications, you’ll never write a line of code specifically because of these changes.

For workloads that live in tight numerical or vectorized loops, however, these improvements can be very real.

And honestly, that’s exactly where I think runtime work like this belongs: in the compiler and runtime, rather than forcing every developer to become an expert in CPU instruction sets just to get decent performance.

4. ReadyToRun Just Got a Surprisingly Big Win
ReadyToRun isn’t exactly the kind of thing that makes for exciting blog material.

It’s one of those runtime features you configure, deploy, and then promptly forget exists.

Which is why this particular .NET 11 improvement caught my attention.

.NET 11 now specializes Comparer<T>.Default and EqualityComparer<T>.Default directly in ReadyToRun (R2R) images. Previously, the default comparers relied on reflection that R2R couldn't fully resolve ahead of time, which could leave the caller falling back to the JIT.

.NET 11 can now generate a specialized helper directly into the R2R image instead.

And Microsoft has a number attached to this one:

Up to 20× faster for the relevant collection operations in their benchmarks.

Now, before anyone reads that and concludes that Dictionary<TKey, TValue> is suddenly 20× faster, no.

That’s not what Microsoft is saying, and we shouldn’t either.

The improvement applies to collection operations that rely on these default comparers, and the actual benefit depends heavily on the workload.

But the underlying change is still pretty interesting.

Why does this matter?
Consider something as ordinary as:

var values = new List<int> { 1, 2, 3, 4, 5 };

if (values.Contains(3))
{
    // ...
}
Operations like these eventually need equality or comparison logic.

For generic code, .NET can use things like:

EqualityComparer<T>.Default
or:

Comparer<T>.Default
The problem wasn’t that these APIs were inherently slow.

The problem was that ReadyToRun couldn’t always see enough information ahead of time to specialize the default comparer the way NativeAOT could.

.NET 11 changes that.

The R2R compiler can now generate the specialized helper ahead of time, avoiding that fallback path. Microsoft describes this as bringing the R2R approach closer to what NativeAOT already does.

And I actually like this improvement quite a bit.

Not because everyone is going to see a 20× speedup.

They won’t.

I like it because it’s attacking a very specific weakness in the AOT story: the runtime knows more about the generic code than the ahead-of-time compiler previously managed to exploit.

.NET 11 closes some of that gap.

And this is another example of why runtime improvements don’t always need a new API.

You don’t have to change:

values.Contains(3);
You don’t have to learn a new optimization technique.

You don’t even have to know that EqualityComparer<T>.Default is involved.

You compile your application with ReadyToRun, and the runtime can now make better use of information it already has.

That’s the kind of optimization I actually want from .NET.

Quiet, boring, and potentially very fast where it matters.

5. Some Runtime Operations Just Got a Lot Faster
Not every runtime improvement needs a new language feature or some fancy new API.

Sometimes Microsoft just makes an operation that you’ve been doing for years cheaper.

.NET 11 has a couple of those that I think are worth calling out.

Interface dispatch gets a big improvement on non-JIT platforms
Interface calls have traditionally needed some extra machinery because the runtime has to figure out which implementation should actually receive the call.

On platforms without JIT support, such as iOS, that could involve a relatively expensive generic fixup path.

.NET 11 adds cached interface dispatch for these scenarios.

Instead of repeatedly going through the expensive path, the runtime can cache the resolved target and reuse it. Microsoft reports improvements of up to 200× in interface-heavy workloads on affected non-JIT platforms.

Obviously, “up to 200×” needs a giant asterisk.

This doesn’t mean your iOS application is suddenly going to run 200× faster.

It means that the interface dispatch operation itself can be dramatically cheaper in workloads where this particular overhead is significant.

Still, I like this one.

Interfaces are everywhere in .NET code. They’re a fundamental part of dependency injection, abstraction, polymorphism, and generic design. If the runtime can make the machinery underneath them cheaper without asking developers to change how they write that code, that’s a worthwhile improvement.

Guid.NewGuid() gets faster on Linux
This one is considerably less dramatic, but it’s a nice example of runtime plumbing getting cleaned up.

On Linux, Guid.NewGuid() now uses the getrandom() system call with batching instead of repeatedly reading from /dev/urandom.

Microsoft reports approximately a 12% throughput improvement for GUID generation.

Will this change how you design your application?

Probably not.

If you’re generating a few GUIDs while handling an HTTP request, you almost certainly don’t care about a 12% improvement here.

But if GUID generation is genuinely part of a hot path, the runtime is now doing less work to get the same result.

And that’s really the theme of these smaller runtime improvements.

You don’t need to learn a new API.

You don’t need to rewrite your code.

You just get a runtime that’s a little less wasteful.

I wouldn’t call either of these the headline feature of .NET 11, but they’re exactly the sort of improvements that make me happy to see in a runtime release. The best optimization is often the one that lets developers keep writing normal code while the runtime quietly gets better at executing it.

6. .NET 11 Can Finally Handle Machines With More Than 1,024 CPUs
Okay, this one is extremely niche. But I think it’s interesting enough to mention. .NET 11 removes a limitation that prevented the runtime from initializing on machines with more than 1,024 logical processors.

Previously, the runtime used sched_getaffinity with the default cpu_set_t, which was limited to 1,024 CPUs. On a sufficiently ridiculous server, that could cause the runtime to fail during initialization..NET 11 changes this by allocating the CPU set dynamically, removing that 1,024-CPU limit for runtime initialization.

There is an important detail, though.

This doesn’t mean .NET suddenly supports more than 1,024 GC heaps. The GC still has its existing 1,024-heap limit. The change is specifically about the runtime being able to initialize and recognize machines with more than 1,024 logical processors.

Will this matter to most of us? Absolutely not.

If your production server has 1,536 logical processors, though, you probably already have a pretty good idea whether this matters to you.

I included it because it’s a nice example of how .NET continues to remove assumptions that made sense when they were introduced but don’t necessarily make sense on today’s increasingly ridiculous hardware.

And honestly, I like seeing the runtime team deal with these edge cases.Just don’t go home tonight and start shopping for a 1,024-core server because of this.

7. One Compatibility Change You Should Know About
This isn’t really an “improvement” in the usual sense, but it’s worth knowing before you casually upgrade a production system to .NET 11.

The minimum hardware requirements are going up.

For x86/x64, .NET 11 moves the minimum baseline from x86–64-v1 to x86–64-v2. On Windows and Linux, ReadyToRun also moves from an x86–64-v2 target to x86–64-v3. Apple keeps its existing ReadyToRun target.

The reason is pretty straightforward.

Supporting very old hardware creates additional complexity for the runtime, while also forcing ahead-of-time compiled code to target a relatively low common denominator.

.NET 11 is raising that floor to take advantage of more modern instruction sets and reduce some of that complexity.

For most developers, this probably changes absolutely nothing.

But if you’re deploying .NET 11 to old servers, unusual hardware, embedded systems, or infrastructure you don’t completely control, this is something you should check before upgrading.

.NET 11 can fail to run on hardware that doesn’t meet the new baseline. That’s really all I want to say about it. It’s important enough to know about, but not important enough to turn this article into a CPU compatibility guide.

Conclusion
.NET 11 has some genuinely interesting runtime work happening under the hood. Runtime Async is the obvious standout, but the JIT, ReadyToRun, SIMD, and smaller runtime optimizations are all pushing things in the same direction: less work, smarter execution, and better use of the hardware underneath us. Most of these changes won’t require you to rewrite a single line of code. And honestly, that’s exactly how I like my runtime improvements.