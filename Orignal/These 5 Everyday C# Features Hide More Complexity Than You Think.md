#These 5 Everyday C# Features Hide More Complexity Than You Think

Introduction
If you’ve been writing C# for a while, you’ve probably noticed that the language rewards familiarity. After a few years, features like expression-bodied members, readonly, default, or even Span<T> become second nature. You stop thinking about them because they simply become part of how you write code.

If you’re not a member, I’ve got you covered! ❤

If you enjoy it, consider clapping, subscribing, or buying me a coffee to show your support! ❤

The interesting part is that familiarity isn’t the same as understanding.

Some of the language features we use every day have subtle behaviors that only become relevant when you’re designing public APIs, chasing down a performance issue, or trying to understand why the compiler rejected code that looked perfectly reasonable. They’re not obscure corners of the language either. They’re features most of us use regularly, often without giving much thought to why they exist or what trade-offs they introduce.

1. Expression-Bodied Members Are Great, Until They Aren’t
Expression-bodied members are one of those features that immediately make code feel cleaner. They remove ceremony from methods, properties, constructors, and other members that simply return or execute a single expression.

public string FullName => $"{FirstName} {LastName}";
public decimal CalculateTax(decimal amount) => amount * TaxRate;
For simple members like these, the syntax is hard to argue against. It removes visual noise and makes the intent obvious. When a method is nothing more than a calculation or a projection, the expression-bodied form is often easier to read than wrapping everything inside braces.

The problem starts when developers become attached to the syntax instead of the readability.

Consider this:

public async Task<Order> GetOrderAsync(int id) =>
    await _repository.GetAsync(id);
Nothing is technically wrong here, but the method is already approaching the point where the expression-bodied syntax adds little value. Now imagine adding logging, validation, exception handling, or metrics. Suddenly the method needs braces anyway, and what started as a concise one-liner becomes something you’re constantly rewriting.

Another common example is squeezing conditional logic into a single expression.

public string GetStatus() =>
    IsActive
        ? HasSubscription
            ? "Premium"
            : "Free"
        : "Inactive";
The compiler has no problem with this. Your teammates probably will.

Expression-bodied members work best when they genuinely represent a single expression whose intent is immediately obvious. They aren’t a badge of modern C#. If converting a member into a block makes it easier to understand, that’s usually the better choice.

Conciseness is valuable. Readability is more valuable.

2. const and readonly Solve Different Problems
At first glance, both keywords seem to accomplish the same goal: making a value immutable.

They don’t.

A const is a compile-time constant.

public const int MaxRetries = 3;
The compiler replaces every usage with the literal value itself.

That has an important consequence.

Imagine you publish a library containing:

public const int DefaultTimeout = 30;
An application references your package and compiles against it.

Later, you change the constant to:

public const int DefaultTimeout = 60;
The application won’t see the new value until it’s recompiled because the original value was baked directly into the consuming assembly during compilation.

readonly behaves differently.

public static readonly TimeSpan DefaultTimeout =
    TimeSpan.FromSeconds(30);
The value is resolved at runtime instead of compile time.

That means updating the library updates the value immediately without forcing every consumer to rebuild.

There’s another limitation.

This won’t compile:

public const DateTime Created = DateTime.UtcNow;
Only compile-time constants can be declared as const.

readonly has no such restriction.

public static readonly DateTime Started =
    DateTime.UtcNow;
The rule is surprisingly simple.

Use const for values that are genuinely universal and will never change, things like mathematical constants or fixed protocol values.

Use readonly for almost everything else.

Many experienced developers default to readonly unless they have a compelling reason to expose a compile-time constant.

3. Binary Literals and Digit Separators Aren’t About Saving Keystrokes
When binary literals and digit separators were introduced, some developers dismissed them as cosmetic syntax.

They’re actually about making intent obvious.

Consider a permissions mask.

const int Read    = 0b0001;
const int Write   = 0b0010;
const int Execute = 0b0100;
const int Delete  = 0b1000;
Reading the bits directly is much easier than mentally converting decimal values.

The same applies to large numeric values.

const int MaxUsers = 1_000_000;
const long FileSize = 10_737_418_240;
Compare that with:

const int MaxUsers = 1000000;
const long FileSize = 10737418240;
The values are identical.

The second version simply forces readers to count digits.

Digit separators also work with hexadecimal and binary values.

const int Color = 0xFF_FF_00;
const int Flags = 0b1010_1100_0001_1110;
The separators have zero runtime cost because the compiler ignores them completely.

This feature isn’t about writing clever code.

It’s about reducing the mental effort required to understand values that already exist.

4. Span<T> Is Fast Because the Compiler Refuses to Trust You
Span<T> is one of the most important performance features added to modern C#.

It lets you work with slices of memory without allocating new arrays or copying data.

Span<int> numbers = stackalloc[] { 1, 2, 3, 4, 5 };
Span<int> firstThree = numbers[..3];
This is incredibly efficient because both spans reference the same underlying memory.

The catch is that this safety comes with strict lifetime rules.

You can’t do this:

public Span<int> GetNumbers()
{
    Span<int> values = stackalloc[] { 1, 2, 3 };
    return values;
}
The compiler rejects it.

Why?

Because stackalloc memory lives on the current stack frame. Once the method returns, that memory disappears. Returning a Span<T> pointing to it would leave callers with a reference to invalid memory.

The same reasoning explains why Span<T> can't be stored in a class field.

public class BufferHolder
{
    private Span<byte> _buffer; // Doesn't compile
}
Or captured by a lambda.

Span<int> values = stackalloc[] { 1, 2, 3 };
Action action = () =>
{
    Console.WriteLine(values[0]);
};
Again, the compiler refuses because the lambda could outlive the stack frame where the span exists.

These restrictions aren’t arbitrary limitations. They’re what make Span<T> both safe and allocation-free. Instead of relying on runtime checks or a garbage collector to prevent invalid memory access, the compiler proves at compile time that a span cannot outlive the memory it references.

Once you understand that principle, most of Span<T>'s rules stop feeling random. They're all enforcing the same guarantee: a span can never outlive its underlying storage.

5. default and new() Look the Same, But They Tell Different Stories
For value types, these two statements produce the same result.

Point p1 = default;
Point p2 = new();
Both variables are zero-initialized.

So why have two ways of writing the same thing?

Because they’re expressing different intent.

When you write:

Point point = default;
You're saying, “Give me the default value for this type.”

When you write:

Point point = new();
you’re saying, “Create a new instance.”

For most structs today, those happen to produce the same result because every field starts with its default value.

The distinction becomes more meaningful in generic code.

public T Create<T>() where T : new()
{
    return new();
}
Here, new() relies on the generic constraint.

By contrast:

public T GetDefault<T>()
{
    return default!;
}
doesn’t require a constructor constraint because every type has a default value.

The runtime behavior is often identical, but the semantics are different.

One communicates default initialization.

The other communicates construction.

Choosing the version that matches your intent makes code easier to understand, especially in generic APIs where those differences become much more significant.

Conclusion
One of the things that makes C# enjoyable to work with is that many of its features feel intuitive. You can be productive without memorizing every detail of the language specification.

But the developers who consistently write clean, maintainable code usually take the extra step of understanding those details anyway.

None of these features will transform a codebase overnight. Together, though, they shape the kind of code that’s easier to maintain, easier to reason about, and easier for the next experienced developer to pick up without surprises. That’s the kind of polish that tends to distinguish good C# code from great C# code.