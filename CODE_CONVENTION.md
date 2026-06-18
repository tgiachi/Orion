# Code Convention — OrionIrcd

This document defines coding conventions for the OrionIrcd project. It is intentionally strict to keep the codebase consistent and readable.

## 1. General Principles

- Prefer clarity over cleverness.
- Keep domain boundaries explicit.
- Keep files small and focused.
- Avoid hidden magic and implicit behavior.
- Write code that is easy to reason about during debugging.

## 2. Project Structure and Namespaces

### 2.1 Folder-to-Namespace Rule

Namespace must match folder path exactly.

```
src/OrionIrcd.Core/Services/ConfigService.cs        → namespace OrionIrcd.Core.Services;
src/OrionIrcd.Service/Subscribers/SocketBroadcastSubscriber.cs → namespace OrionIrcd.Service.Subscribers;
tests/OrionIrcd.Tests/Core/EventBusServiceTests.cs  → namespace OrionIrcd.Tests.Core;
```

### 2.2 Domain-First Organization

Group by domain first, not by technical suffix.

### 2.3 Mandatory Namespace Buckets

| Bucket | Content |
|---|---|
| `Types` | Enums, type constants (domain-prefixed) |
| `Data` | DTOs, records, simple data carriers |
| `Data.Config` | Configuration models |
| `Data.Notifications` | Notification DTO |
| `Data.Internal.*` | Internal-only data models |
| `Interfaces` | Contracts only |
| `Services` | Service implementations |
| `Internal` | Implementation details not part of public API |
| `Subscribers` | `IEventBus` subscriber classes |
| `DBus` | D-Bus interface definitions |

## 3. C# File and Type Rules

- One `.cs` file must contain at most one primary type (`class`, `record`, or `enum`).
- File name must match type name.
- Use file-scoped namespaces.
- Do **not** use primary constructors.
- Do **not** use expression-bodied constructors (`public X(...) => ...`); constructors must always have a body `{ }`.

## 4. Class Layout Order

Inside a type, use this order:

1. `const` fields
2. `private readonly` fields (prefixed `_`)
3. Non-readonly fields
4. Properties
5. Constructor(s)
6. Public methods
7. Protected methods
8. Private methods
9. `Dispose`/finalization methods (always last)

### 4.1 Private Readonly Naming

All `private readonly` fields must start with `_`:

```csharp
private readonly IEventBus _eventBus;
private readonly DirectoriesConfig _directoriesConfig;
```

### 4.2 Dispose Position

If a class implements `IDisposable` or `IAsyncDisposable`, `Dispose`/`DisposeAsync` must be the last method(s) in the file.

## 5. Interfaces

- Interfaces live only under `Interfaces` namespaces.
- Every interface must have XML docs (`///`).
- Interface names must use `I` prefix and clear domain naming.

## 6. Enums

- Enums must live under a `Types` namespace for their domain.
- Always include the domain in the enum name.

```csharp
// Types/LogLevelType.cs
namespace OrionIrcd.Core.Types;
public enum LogLevelType { ... }

// Types/DirectoryType.cs
namespace OrionIrcd.Core.Types;
public enum DirectoryType { Scripts, Logs, Plugins, Configs }
```

## 7. Strings

- Always use `string.Empty` instead of `""`.

## 8. Logging

- Use Serilog **statically** via `Log.ForContext<T>()`. Do not inject `ILogger<T>` via DI.
- Declare the logger as a `private readonly` field initialized inline.

```csharp
private readonly ILogger _logger = Log.ForContext<MyService>();
```

- When both Serilog and Microsoft.Extensions.Logging are in scope (e.g., `OrionIrcd.Service` which uses `Microsoft.NET.Sdk.Web`), add a using alias to resolve the ambiguity:

```csharp
using Serilog;
using ILogger = Serilog.ILogger;
```

- Use static message templates; never use string interpolation for structured logs.
- Keep template shape stable across calls.

## 9. Event Bus

- All event types must implement `IOrionIrcdEvent`.
- Use `IEventBus.Subscribe<T>` to register handlers; use `IEventBus.PublishAsync<T>` to emit events.
- Subscriber classes live in `Subscribers/` and register themselves in the constructor.

```csharp
// Pattern: SocketBroadcastSubscriber
internal class MySubscriber
{
    public MySubscriber(IEventBus eventBus, ...)
    {
        eventBus.Subscribe<Notification>(HandleAsync);
    }
}
```

## 10. Plugin System

- Plugins implement `ISourcePlugin` (Id in reverse-domain format: `com.github.author.OrionIrcd.plugins.name`).
- Plugins receive an `IPluginContext` — use `context.EventBus` to publish, `context.Logger` to log, `context.ConfigPath` for config.
- Plugin hosts are loaded via `PluginLoadContext` (`AssemblyLoadContext(isCollectible: true)`) for hot-reload support.

## 11. D-Bus Interfaces

- D-Bus proxy interfaces live in `DBus/` and must be `public` (required by Tmds.DBus runtime proxy generation via Reflection.Emit).
- Annotate with `[DBusInterface("...")]` and inherit `IDBusObject`.

## 12. Hosted Services / Subscribers

- Background services implement `IHostedService` (or extend `BackgroundService`).
- If an optional external dependency (e.g., D-Bus session bus) is unavailable at startup, log a `Warning` and return cleanly — do not crash the host.
- Subscribers that are not hosted services are registered as singletons and force-resolved in `Program.cs` to trigger constructor subscription registration.

## 13. Test Conventions

### 13.1 Structure

```
tests/OrionIrcd.Tests/<Domain>/<Subdomain>/<SubjectName>Tests.cs
namespace OrionIrcd.Tests.<Domain>.<Subdomain>;
```

Examples:
```
tests/OrionIrcd.Tests/Core/EventBusServiceTests.cs   → namespace OrionIrcd.Tests.Core;
tests/OrionIrcd.Tests/Service/UnixSocketServerTests.cs → namespace OrionIrcd.Tests.Service;
tests/OrionIrcd.Tests/Support/FakeSourcePlugin.cs    → namespace OrionIrcd.Tests.Support;
```

### 13.2 Naming

- File: `<SubjectName>Tests.cs`
- Class: `<SubjectName>Tests`
- One main test class per file.
- Test method style: `Method_Scenario_ExpectedResult`.

### 13.3 Test Support

- Shared fakes, builders, and helpers go in `tests/OrionIrcd.Tests/Support/`.
- Do not mix reusable test infrastructure into domain test files.

### 13.4 InternalsVisibleTo

`OrionIrcd.Service.csproj` exposes internals to `OrionIrcd.Tests` via:
```xml
<InternalsVisibleTo Include="OrionIrcd.Tests"/>
```

## 14. Commits

- Use Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, etc.).
- Scope commits to the affected subsystem: `feat(dbus):`, `fix(socket):`, `test(eventbus):`.
- Never add `Co-Authored-By: Claude` to commits.

## 15. Non-Negotiable Hygiene

- No dead code.
- No TODO comments without a tracked follow-up.
- No inconsistent naming across domains.
- Keep warnings under control; do not normalize noisy warnings.
- No `""` for empty strings - use `string.Empty`.
- No primary constructors.
- No expression-bodied constructors.

## 16. Additional Conventions

**Nullability**
- Use nullable reference types consistently.
- Avoid null-forgiving (`!`) unless explicitly justified.

**Async naming**
- Async methods must end with `Async`.
- Include `CancellationToken` on I/O-bound public async methods.

**Exception handling**
- Use guard clauses for external inputs and public API validation.
- Do not use `ArgumentNullException.ThrowIfNull` for constructor dependencies resolved by DI; let the container enforce required services.
- Do not swallow exceptions silently.

**Collection exposure**
- Expose `IReadOnlyList<>` or `IReadOnlyDictionary<>` where mutation by callers is not intended.

**Test naming**
- Prefer `Method_Scenario_ExpectedResult`.
- Keep tests focused on a single behavior.

**No magic numbers**
- Replace protocol/timing literals with named constants.

**Using directives**
- Keep usings ordered: system first, then third-party, then project namespaces.
- Add using aliases when a name is ambiguous across two libraries in scope.
