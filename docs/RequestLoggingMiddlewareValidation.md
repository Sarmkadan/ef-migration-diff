# RequestLoggingMiddlewareValidation

The `RequestLoggingMiddlewareValidation` static class provides validation helpers for
[`RequestLoggingMiddleware`](RequestLoggingMiddleware.md) instances. It exposes extension methods
that check whether a middleware instance is correctly configured for use, returning a list of
human-readable problems when validation fails. The class is designed to be used both as a
defensive guard before invoking the middleware and as a diagnostic tool for reporting
configuration issues.

## Purpose

`RequestLoggingMiddleware` is a command middleware that logs command invocation details
(arguments, execution time, results) via an injected `ILogger`. Because it can be constructed
with optional dependencies (`ILogger`, verbosity flag), callers may pass an instance that is not
fully configured. `RequestLoggingMiddlewareValidation` centralizes the checks that determine
whether such an instance is safe to use, so the same rules are enforced consistently across the
codebase instead of being duplicated at each call site.

## API

### `public static IReadOnlyList<string> Validate(this RequestLoggingMiddleware value)`

Runs all validation rules against the specified middleware instance and returns the list of
problems found.

- **Parameters**: `value` – The middleware instance to validate.
- **Returns**: A `IReadOnlyList<string>` of human-readable validation problems; empty if the
  instance is valid.
- **Throws**: `ArgumentNullException` if `value` is `null`.

### `public static bool IsValid(this RequestLoggingMiddleware value)`

Convenience predicate that reports whether the instance passes validation.

- **Parameters**: `value` – The middleware instance to check.
- **Returns**: `true` if the instance is valid; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `value` is `null`.

### `public static void EnsureValid(this RequestLoggingMiddleware value)`

Throws if the instance is not valid. Use this as a guard at the start of a method that requires a
properly configured middleware.

- **Parameters**: `value` – The middleware instance to validate.
- **Throws**: `ArgumentNullException` if `value` is `null`.
- **Throws**: `ArgumentException` if `value` is not valid, with a message containing the joined
  list of problems.

## Validation rules

The validation rules enforced by this class are:

1. **Non-null instance** — the middleware instance must not be `null`. This is enforced by every
   method via `ArgumentNullException.ThrowIfNull(value)`.
2. **No configuration problems** — the instance must not produce any validation problems. The
   `Validate` method currently returns an empty problem list, meaning a non-null instance is
   always considered valid. The rule is expressed as "the list of problems must be empty", so
   future configuration checks can be added to `Validate` without changing the calling contract.

## Usage

### Example 1: Guarding a method that requires a valid middleware

```csharp
public void Run(RequestLoggingMiddleware middleware)
{
    middleware.EnsureValid();
    // Safe to use middleware here.
}
```

### Example 2: Reporting problems instead of throwing

```csharp
var problems = middleware.Validate();
if (problems.Count > 0)
{
    Console.WriteLine($"Middleware is misconfigured: {string.Join(" ", problems)}");
}
```

### Example 3: Conditional check

```csharp
if (middleware.IsValid())
{
    await middleware.InvokeAsync(context);
}
```

## Notes

- All methods are extension methods on `RequestLoggingMiddleware`, so they can be called directly
  on an instance without a separate static import beyond the namespace.
- `IsValid` is implemented in terms of `Validate` (`Validate().Count == 0`), and `EnsureValid` is
  implemented in terms of `Validate` as well, so all three methods always agree on the same set of
  rules.
- The validation contract is intentionally open-ended: `Validate` returns a list so that multiple
  problems can be reported at once, and the empty-list result means "valid". This keeps the API
  stable if additional configuration invariants are introduced later.