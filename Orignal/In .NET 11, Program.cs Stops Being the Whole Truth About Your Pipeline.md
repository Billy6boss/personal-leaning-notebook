# In .NET 11, Program.cs Stops Being the Whole Truth About Your Pipeline
For ten years, the ASP.NET Core pipeline had one thing that made it easy to reason about: it was explicit. Whatever we called with app.Use() ran, in that order, and nothing else did. We could hand someone Program.cs, and they could tell you exactly what happened to a request before it reached your handler.

.NET 11 ends that. As of Preview 6, an app built with WebApplication.CreateBuilder has cross-site request forgery middleware wired into the pipeline, whether you registered it or not. Kestrel parses malformed requests through a different code path than it did in .NET 10. The framework emits OpenTelemetry attributes on its own. Validation can now suspend a request on a database call before your endpoint body executes.

Four changes, and only one of them is worth more than a few paragraphs. The CSRF middleware is the one that changes behavior you can’t see from your own code…

Everything below is against .NET 11 Preview 6, which shipped on July 14. GA is November 10. Preview APIs move, and at least one of these already got reshaped between previews.

Press enter or click to view image in full size

Image from https://devblogs.microsoft.com/dotnet/wp-content/uploads/sites/10/2026/08/dotnet11p7.webp
The CSRF middleware you didn’t register
The whole setup:

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/widgets", ([FromForm] Widget w) =>
    Results.Created($"/widgets/{w.Id}", w));
app.Run();
That app is CSRF-protected in .NET 11. No AddAntiforgery(), no UseAntiforgery(), no token threading, no data protection dependency. A form post from https://attacker.example.com gets a 400 before the handler body runs. A same-origin post from your own page works. A curl request works.

The mechanism is not the token system. There is no synchronizer token, no cookie pair, no encrypted payload. The middleware reads two headers the browser attaches and decides from those alone.

The decision chain
The middleware runs after authentication and authorization, and walks a short ordered list. First match wins:

GET, HEAD, OPTIONS, TRACE — allowed. Safe methods shouldn't be changing state anyway.
Sec-Fetch-Site: same-origin or Sec-Fetch-Site: none — allowed. This is where the overwhelming majority of real traffic exits. same-origin covers in-app navigation and fetch; none covers a user typing a URL or hitting a bookmark.
An Origin the endpoint's resolved CORS policy trusts — allowed.
Any other Sec-Fetch-Site value — denied. That means cross-site and, notably, same-site. A subdomain is not the same origin.
No Sec-Fetch-Site but an Origin present — compare Origin against scheme://host[:port] from the request. Match allows, mismatch denies. This is the legacy-browser path, for anything predating the Fetch Metadata spec around 2020.
Neither header — allowed.
Rule 6 looks like a hole and isn’t. Browsers always send at least one of these on a write request. Something sending neither is curl, Postman, a mobile app, or another service. CSRF is an attack that borrows the browser's ambient cookies; a non-browser client that wants to abuse your API doesn't need the trick, it just calls you with whatever credentials it has.

The reason this works at all without a token is that Sec-Fetch-Site and Origin are forbidden request headers. Script in the page cannot set or overwrite them — the browser owns those values and rejects attempts to forge them. A malicious page cannot dress a cross-origin POST up as same-origin. That property is what buys you token-grade assurance with no server state, no data protection keys, and no key-ring rotation problem across instances.

I think this is the right trade. The token system’s dependency on data protection has been a persistent source of multi-instance pain, and the “multiple tabs” failure mode never had a clean answer.

The verdict is recorded, not enforced
This is the part that matters, and the part I expect most upgrade notes to flatten into “CSRF is on by default now.”

The middleware does not reject anything. It records a verdict on the request’s IAntiforgeryValidationFeature, the same feature the token system has always used, and lets the request continue down the pipeline. A denied verdict becomes a 400 only when something further along reads it.

Four things read it: MVC actions under antiforgery, minimal API endpoints that bind a form parameter, Blazor static SSR form posts, and any code that touches the request form directly, which acts as a backstop.

Notice what’s missing. An endpoint that never reads form data executes normally even when the verdict on the request is invalid. A MapPost binding JSON from the body. A [HttpPost] Web API action. A handler that ignores the body entirely. The verdict sits there on the feature, correctly marked invalid, and nothing acts on it.

Microsoft’s own migration doc puts it plainly: endpoints that bind JSON have no behavioral change.

That is a defensible design. Classic CSRF depends on a browser being able to submit a form cross-origin with ambient cookies attached, and a JSON endpoint with a Content-Type: application/json requirement is not reachable that way without a preflight that CORS already governs. But "defensible" is not "protected," and the distinction is going to get lost. The body types that skip preflight are form-encoded and text/plain. If you have a cookie-authenticated endpoint that accepts either, read the verdict yourself rather than assuming a default caught it.

What actually breaks
One scenario, and it’s specific: a browser-based client posting a form to a different origin. Your SPA at app.contoso.com posting to api.contoso.com. Sec-Fetch-Site on that request is same-site, not same-origin, so rule 4 denies it, and the form binding turns that into a 400 with an empty body.

The fix is CORS, not an opt-out. The CSRF middleware doesn’t maintain its own trust list — it reuses whatever CORS policy resolves for the endpoint, resolved the same way the CORS middleware resolves it: named policy from [EnableCors("api")] or .RequireCors("api") first, then the default policy, then nothing.

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://app.contoso.com")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
AllowAnyOrigin is deliberately not honored as a trust signal. That's the single best decision in this whole feature. AllowAnyOrigin means "anyone may read this resource," which is a completely different claim from "anyone may mutate state on this user's behalf," and treating them as equivalent would have made the middleware a no-op for every app with a permissive read policy. If you need public reads and protected writes, list your write origins explicitly.

[DisableCors] is also not an opt-out. It only skips the CORS-derived trust step; the request still has to satisfy the Sec-Fetch-Site and origin-vs-host rules. To genuinely opt an endpoint out, use .DisableAntiforgery() on a minimal API endpoint or group, or [IgnoreAntiforgeryToken] on an MVC action. Both write IAntiforgeryMetadata { RequiresValidation = false }, and both protections honor it. Do that only for endpoints that aren't browser-reachable or don't authenticate with cookies.

If you already use tokens, nothing changes
Call app.UseAntiforgery() and the token middleware runs after the CSRF middleware, clears whatever verdict it recorded, and writes its own. The token result wins in both directions: a request the CSRF middleware denied becomes valid with a good token, and a request it allowed becomes invalid without one.

So apps on the token system see identical end-to-end behavior to .NET 10. Apps that never called UseAntiforgery() fall through to the new verdict. That ordering is why this shipped as a default at all without breaking half the ecosystem.

Blazor static SSR is the one place with a real behavior change. Apps that had removed app.UseAntiforgery() are now protected by the middleware instead of running unprotected, and they stop emitting antiforgery tokens for rendered forms — there's no token middleware left to validate them on the way back in. If you deliberately stripped antiforgery out of a Blazor SSR app, read the breaking-change notice before you bump the TFM.

Seeing it work
Every invalid verdict logs at Debug under Microsoft.AspNetCore.Antiforgery.CsrfProtectionMiddleware, event name CsrfValidationFailed. Turn it on in development:

{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Antiforgery.CsrfProtectionMiddleware": "Debug"
    }
  }
}
And you can reproduce a cross-origin post locally without a second origin, because curl with an explicit Origin header lands on rule 5:

curl -i -X POST https://localhost:7042/widgets \
     -H "Origin: https://attacker.example.com" \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "name=test"
Drop the Origin header and the same request succeeds, because now it's neither-header traffic and rule 6 waves it through. That pair of commands is the fastest way to convince a skeptical reviewer that the middleware is doing what you say it is.

There’s a global kill switch, DisableCsrfProtection, settable in appsettings.json or as ASPNETCORE_DisableCsrfProtection. It has a trap attached. The automatic middleware also satisfies the antiforgery requirement for endpoints that demand validation. If your app relies on antiforgery but never calls app.UseAntiforgery(), killing the CSRF middleware globally leaves those endpoints with no antiforgery middleware at all, and a request to one throws. Same applies if you're not building through WebApplication in the first place. Prefer per-endpoint opt-outs.

Async validation, and the sync method you still have to write
The other change with real depth. System.ComponentModel.DataAnnotations picked up AsyncValidationAttribute, IAsyncValidatableObject, and Validator.ValidateObjectAsync, and Microsoft.Extensions.Validation now runs them when an endpoint validates a request.

Concretely: a uniqueness check against the database can live in the validation layer instead of the first four lines of your handler. Derive from AsyncValidationAttribute, implement the async check, and it runs before the endpoint body.

Object-level rules implement IAsyncValidatableObject and stream results back:

public class ReservationRequest : IAsyncValidatableObject
{
    [Required] public string Email { get; set; } = "";
    public DateOnly Date { get; set; }
    
    // This type validates asynchronously only.
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx) =>
        throw new InvalidOperationException(
            "ReservationRequest validates asynchronously. Use ValidateAsync.");
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext ctx,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var calendar = ctx.GetRequiredService<IBookingCalendar>();
        if (!await calendar.IsOpenAsync(Date, ct))
            yield return new ValidationResult("Closed on that date.", [nameof(Date)]);
    }
}
The sharp edge is that throw. IAsyncValidatableObject extends IValidatableObject, so the synchronous Validate is still on the interface and you still have to implement it. Minimal API validation always calls the async path and never the sync one — the two are not meant to run together. But other callers into Validator will happily take the sync path, and if you left it returning an empty sequence, your validation gets silently skipped by those callers with no error anywhere. Throwing is the correct implementation for an async-only type, and it's the kind of thing that looks like a bug to whoever reads it next. Leave a comment.

This removes one of the two main reasons teams reach for FluentValidation. Attribute-based validation that couldn’t await was a real limitation, and “just use FluentValidation” was the standard answer. That answer is now weaker than it was in June.

Three more, briefly
These matter less to how you write code, so they get a paragraph each.

Kestrel’s HTTP/1.1 parser stopped throwing on malformed requests. It returns a result struct carrying success, incomplete, or error instead of a BadHttpRequestException per parse failure. Under port scanning, hostile traffic, or a misconfigured client hammering you with garbage, that's a 20–40% throughput improvement, and valid request processing is untouched. Exceptions were never expensive because of the try/catch; they were expensive because of stack unwinding and allocation on a path that, for an internet-facing service, is not exceptional at all.

ASP.NET Core now writes OpenTelemetry semantic-convention attributes onto the HTTP server activity itself. Method, path, status, server address — all populated by the framework. Subscribe to the Microsoft.AspNetCore source and drop OpenTelemetry.Instrumentation.AspNetCore entirely. There's an AppContext switch, Microsoft.AspNetCore.Hosting.SuppressActivityOpenTelemetryData, if you need it off. I'm giving this one paragraph because it deserves a whole article and I'd rather write that one properly than bolt three sentences onto this one.

OpenAPI 3.2 is the default document version now. If anything downstream of you pins 3.1 or 3.0 — a code generator, a gateway, a contract test — check it before the TFM bump rather than after.

What to actually do
The through-line is that framework defaults have become opinions, and opinions are things you have to read. Program.cs will tell you what you registered. It will no longer tell you what runs.

Practically, before November: build against a preview, turn on Debug for the CSRF middleware category, and drive your normal integration suite plus whatever cross-origin flows you have. Every CsrfValidationFailed line is either a real cross-origin form post you need to allow via WithOrigins, or an endpoint you should be explicit about opting out. Then go find your cookie-authenticated endpoints that accept anything other than JSON, because those are the ones sitting in the gap between "the verdict was recorded" and "something enforced it."

The default is better than what most teams had. It just isn’t the same thing as “protected.”