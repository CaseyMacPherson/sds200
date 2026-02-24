# GitHub Copilot Instructions for SDS200v2

This file provides AI-specific guidance for GitHub Copilot when working in this repository.

## Project Context

This is a **.NET 10 CLI application** for controlling Uniden SDS200 scanners via Serial or UDP. It uses **Spectre.Console** for the terminal UI and follows a **layered architecture**.

## Required Reading

Before making suggestions, review:
- **Full design guide:** [`docs/DESIGN_GUIDE.md`](../docs/DESIGN_GUIDE.md)
- **Protocol spec:** [`docs/SDSSerialAndUDPProtocol.md`](../docs/SDSSerialAndUDPProtocol.md)

## Key Architecture Layers

```
┌─────────────────────────────────────┐
│  Presentation (Spectre.Console)     │  MainViewRenderer, DebugViewRenderer, etc.
├─────────────────────────────────────┤
│  Logic (Business Rules)             │  UnidenParser, ContactTracker, KeyboardHandler
├─────────────────────────────────────┤
│  Bridges (Protocol Adapters)        │  SerialScannerBridge, UdpScannerBridge
├─────────────────────────────────────┤
│  Core (Interfaces)                  │  IScannerBridge, IDataReceiver
└─────────────────────────────────────┘
```

**NEVER mix layers** — e.g., don't put Spectre.Console markup in `Logic/` classes.

## Critical Rules

### 1. Code Style

- **All public members** must have XML documentation (`/// <summary>`)
- **Private fields** use `_camelCase`
- **No magic strings** — centralize in `MarkupConstants.cs`
- **Thread-safe queues** — use `ConcurrentQueue<T>` for cross-thread data
- **Escape user input** before Spectre.Console markup: `Markup.Escape(userInput)`

### 2. Error Handling

- **Transient failures** return sentinel values (`"TIMEOUT"`, `"DISCONNECTED"`)
- **Fatal errors** fail fast with clear messages via `AnsiConsole.MarkupLine`
- **Catch specific exceptions** (`SocketException`, `OperationCanceledException`)
- **Never** swallow exceptions silently

### 3. Async/Threading

- **No blocking** on async code — use `await`, never `.Result` or `.Wait()`
- **Keyboard polling** uses `Console.KeyAvailable` (non-blocking)
- **Cancel tokens** for graceful shutdown
- **Lock critical sections** with `lock (_lock)` for state updates

### 4. Protocol Compliance

- **All commands** end with `\r`
- **Normalize to uppercase** before sending to scanner
- **UDP multi-packet XML** only for `GLT` commands (uses `<Footer No EOT/>`)
- **Timeout defaults:** 500ms for simple commands, 5-10s for multi-packet

### 5. UI Consistency

Use the established color palette:
- **Green:** Frequency, success states
- **Blue:** System/Department labels
- **Yellow:** TGID/Channel, warnings
- **Red:** Errors, holds, disconnected
- **Grey/Dim:** Metadata (UID, volume, squelch)

All UI strings come from `MarkupConstants.cs`.

## Common Tasks

### Adding a New Scanner Command

1. Add to `UnidenParser.cs` if it updates status
2. Add response handler in appropriate `Parse{Mode}()` method
3. Update `ScannerStatus.cs` if new properties needed
4. Add test case in `SDS200.Cli.Tests/UnidenParserTests.cs`

### Adding a New View Mode

1. Add enum to `ViewMode.cs`
2. Create `{Name}ViewRenderer.cs` in `Presentation/`
3. Update `RenderView()` in `Program.cs`
4. Add hotkey in `KeyboardHandler.cs`
5. Add constants to `MarkupConstants.cs`

### Adding Network Features (e.g., RTSP Audio)

1. Keep **separate from command channel** (different port/socket)
2. Create service class in `Logic/` (e.g., `RtspAudioPlayer`)
3. Wire into `UdpScannerBridge` with explicit lifecycle methods
4. Guard with `if (bridge is UdpScannerBridge)` — network-only
5. Add keyboard toggle and UI indicator

## What NOT to Suggest

- ❌ **Dependency injection containers** (keep dependencies explicit)
- ❌ **ORMs or Entity Framework** (simple JSON for settings)
- ❌ **Async void methods** (except event handlers)
- ❌ **Global static state** (pass dependencies via constructors)
- ❌ **Mixing presentation and logic** (respect layer boundaries)
- ❌ **Complex LINQ chains** (prefer clarity over cleverness)
- ❌ **ConfigureAwait(false)** (application code, not library)

## File Organization

| Folder          | Purpose                          | Examples                          |
|-----------------|----------------------------------|-----------------------------------|
| `Core/`         | Interfaces and contracts         | `IScannerBridge.cs`               |
| `Bridges/`      | Transport implementations        | `UdpScannerBridge.cs`             |
| `Logic/`        | Business logic, services         | `UnidenParser.cs`                 |
| `Models/`       | Data structures (POCOs)          | `ScannerStatus.cs`                |
| `Presentation/` | Spectre.Console rendering        | `MainViewRenderer.cs`             |
| `Tests/`        | xUnit test project               | `UnidenParserTests.cs`            |

## Examples of Good vs Bad Code

### ✅ GOOD: Layered, documented, testable

```csharp
/// <summary>
/// Processes a signal update and tracks contact state.
/// Creates a new contact entry when signal locks.
/// </summary>
public void ProcessSignalUpdate(ScannerStatus status)
{
    bool signalPresent = status.LastRssiValue > 0;
    
    if (signalPresent && !status.SignalLocked)
    {
        status.SignalLocked = true;
        var entry = ContactLogEntry.FromStatus(status);
        EnqueueCapped(entry);
    }
}
```

### ❌ BAD: No docs, global state, mixing concerns

```csharp
public static void Update(object data)
{
    GlobalState.Status = (ScannerStatus)data;
    Console.WriteLine($"FREQ: {GlobalState.Status.Frequency}"); // Direct console access!
}
```

## Quick Checklist for Suggestions

When generating code, ensure:

- [ ] XML documentation on public members
- [ ] Follows naming conventions (`_privateField`, `PublicProperty`)
- [ ] Appropriate layer (Core/Bridge/Logic/Presentation)
- [ ] Thread-safety for shared state
- [ ] No Spectre.Console code in `Logic/` or `Models/`
- [ ] User input escaped before markup
- [ ] Protocol commands end with `\r`
- [ ] Error handling with clear messages

## Testing

- Use **xUnit** framework
- **Arrange-Act-Assert** pattern
- Test **parsing logic** and **business rules** (high priority)
- Test **UI rendering** via visual inspection (low priority)
- Mock `IScannerBridge` for integration tests

---

**When in doubt, prioritize:**
1. **Clarity** over cleverness
2. **Testability** over brevity
3. **Protocol accuracy** over assumed behavior
4. **Explicit dependencies** over magic

For detailed rationale and more examples, see [`docs/DESIGN_GUIDE.md`](../docs/DESIGN_GUIDE.md).

