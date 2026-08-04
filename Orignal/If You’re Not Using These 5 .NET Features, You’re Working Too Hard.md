# If You’re Not Using These 5 .NET Features, You’re Working Too Hard

That’s when you realize the framework already has solid tools for these problems, but hardly anyone talks about them.

In this post, I’m going to walk you through a handful of APIs and patterns that feel like “hidden gems.” These aren’t shiny new features; they’re the kind of tricks that make you look like you’ve been around the block a few times.

Let’s get right into it!

1. Stop Using RuntimeInformation Hacks: OperatingSystem.IsX Is the Cleaner Way
For years, checking the current OS in .NET meant writing something like this:

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Windows-specific code
}
It works, but it is verbose and prone to mistakes if you start juggling multiple platforms. Then came OperatingSystem.IsWindows, OperatingSystem.IsLinux, and OperatingSystem.IsMacOS. These are simple, direct, and less error-prone:

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("Running on Windows");
}
The real win here is readability. When someone else looks at your code, they don’t need to know what OSPlatform enum values are. They just see what you meant: “Run this if it’s Windows.” It’s one of those small upgrades that make your code look modern and intentional.

2. Plugin Isolation Done Right with AssemblyLoadContext
If you’ve ever tried building a plugin system in .NET, you know the pain of assembly conflicts. Load two versions of the same dependency, and suddenly nothing works. The default AppDomain loading model wasn’t built for this kind of extensibility.

That’s where AssemblyLoadContext comes in. It lets you load assemblies in isolation, so two plugins can each depend on different versions of the same DLL without stepping on each other.

Here’s the bare minimum to get started:

class PluginLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver _resolver;
    
    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}
With this in place, each plugin lives in its own world. You can unload them cleanly, too, which is huge for long-running processes like servers. It’s a serious upgrade over the old “just drop DLLs in a folder and pray” approach.

3. Untangling Dependencies with AssemblyDependencyResolver
Now, you saw it sneak into the previous example, but it deserves its own spotlight. AssemblyDependencyResolver is the unsung hero when working with custom loaders. Given a path to a plugin or assembly, it figures out where the dependencies should come from.

Without it, you’d be writing brittle manual logic like “look in this folder, then that folder, then maybe check the GAC.” With it, you just say:

var resolver = new AssemblyDependencyResolver(pluginPath);
var path = resolver.ResolveAssemblyToPath(assemblyName);
And you’re done. You get the correct dependency resolution rules, consistent with how .NET would normally do it, but scoped to your plugin context. That’s how you avoid nasty surprises when your plugin references Newtonsoft.Json 13.0.1 and your host app uses 12.0.3.

4. Getting Assembly Versions Without Shooting Yourself in the Foot
If you’ve ever written:

var version = Assembly.GetExecutingAssembly().GetName().Version;
You probably think you’re safe. But here’s the catch: this value isn’t always the same as the version you stamped in your project. It’s the assembly version, not necessarily the file version or informational version. Which one do you actually want?

AssemblyName.Version gives you the assembly version baked at compile time.
FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion gives you the file version.
AssemblyInformationalVersionAttribute can give you semantic versions like 1.0.0-beta+sha.abc123.
So the better approach is to be intentional. If you care about semantic versioning, fetch the informational version:

var version = Assembly
    .GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion;
That way, you get the actual string you meant to communicate, instead of whatever default MSBuild happened to stamp on your DLL.

5. Let the DI Container Do the Heavy Lifting with ActivatorUtilities
When you’re knee-deep in dependency injection, you’ll sometimes hit a wall where you need to create an object that isn’t registered in the container, but still has dependencies that are. The amateur move is to resolve all those dependencies manually and instantiate the object yourself.

Instead, use ActivatorUtilities:

var myService = ActivatorUtilities.CreateInstance<MyService>(serviceProvider);
This tells the container, “I know MyService isn’t registered, but please inject everything it needs from the services you do know about.” It’s a clean escape hatch that saves you from wiring up awkward factories or littering your code with service resolution logic.

The more you use DI, the more you’ll run into situations where this makes your life easier. It’s not about replacing registrations, it’s about flexibility when the standard patterns don’t quite fit.

Conclusion
Most developers get by without ever touching things like AssemblyLoadContext, ActivatorUtilities, or OperatingSystem.IsWindows. And that’s fine until the day their app starts misbehaving in production and they’re stuck rewriting half the system to fix something subtle.

The point of learning these APIs isn’t to flex obscure knowledge. It’s to save yourself from pain later. The next time you need to support plugins, resolve versioning conflicts, or spin up an object with DI dependencies without rewriting your container, you’ll know there’s already a solution baked into .NET.

This is the difference between just writing code and writing code that’s built to last.