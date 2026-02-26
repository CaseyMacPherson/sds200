# SDS200v2 Design Guide & Architectural Principles

**Version:** 1.0  
**Last Updated:** February 23, 2026  
**Audience:** AI assistants, contributors, and maintainers

This document captures the architectural patterns, coding standards, and design principles for the SDS200v2 CLI project. Follow these guidelines to ensure consistency and maintainability.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Core Architecture Principles](#core-architecture-principles)
3. [Project Structure & Organization](#project-structure--organization)
4. [Code Style & Conventions](#code-style--conventions)
5. [Design Patterns](#design-patterns)
6. [Threading & Concurrency](#threading--concurrency)
7. [Error Handling](#error-handling)
8. [Testing Philosophy](#testing-philosophy)
9. [Documentation Standards](#documentation-standards)
10. [UI/UX Guidelines](#uiux-guidelines)
11. [Extension Points](#extension-points)

---

## Project Overview

**SDS200v2** is a cross-platform .NET 10 CLI application that communicates with Uniden SDS200 scanners via **Serial (USB)** or **UDP (Network)** interfaces. It provides real-time status monitoring, contact logging, and remote control through a terminal-based UI built with **Spectre.Console**.

**Key Goals:**
- **Cross-platform compatibility** (macOS, Linux, Windows)
- **Clean separation of concerns** (presentation, logic, I/O)
- **Testable architecture** with minimal dependencies
- **Protocol accuracy** following Uniden SDS200 specifications
- **Responsive TUI** with minimal input lag

---

## Core Architecture Principles

### SOLID Principles (Mandatory)

#### **S — Single Responsibility Principle**
Each class should have ONE reason to change. Split responsibilities clearly. Each class should do exactly one thing: parsing (like `GsiResponseParser`), updating state (like `ScannerStatusUpdater`), or detecting events (like `ContactDetector`).

Avoid creating "god classes" that handle multiple concerns like parsing, UI updates, contact logging, and command sending all at once.

**AI Agent Guidance:** When asked to add functionality, first identify which single-purpose class should own it. If none exist, create a new focused class rather than extending an existing one beyond its responsibility.

#### **O — Open/Closed Principle**
Classes should be open for extension, closed for modification. Use abstraction to add behavior without changing existing code. Define an interface like `IResponseParser` with implementations like `GsiParser`, `MdlParser`, and `PsiParser`. A `ParserChain` can automatically handle new parsers without modification to existing code.

Avoid modifying a single static `Parser` class by adding if-else branches for each new command type.

**AI Agent Guidance:** When adding support for new command types, create new parser classes implementing `IResponseParser` rather than adding switch cases or if-else chains.

#### **L — Liskov Substitution Principle**
Derived classes must be substitutable for their base types without breaking behavior. All bridge implementations (`SerialScannerBridge`, `UdpScannerBridge`) should honor the `IScannerBridge` contract identically. They should all successfully implement `SendAndReceiveAsync` with the same expectations.

Never throw `NotImplementedException` in production code. For testing, use proper mocking frameworks instead of fake implementations that violate the contract.

**AI Agent Guidance:** All implementations of an interface must honor its contract fully. Never throw `NotImplementedException` in production code. For testing, use proper mocking frameworks.

#### **I — Interface Segregation Principle**
Clients should not depend on interfaces they don't use. Create focused interfaces like `IScannerConnection` (connection concerns), `IScannerCommandSender` (command sending), and `IScannerEventSource` (events). Compose these into a larger `IScannerBridge` interface.

Avoid creating "fat" interfaces that force all implementations to handle features they don't need. For example, don't require all bridges to implement `StartAudioStream()` if only UDP supports audio.

**AI Agent Guidance:** When adding features, evaluate if they belong in the existing interface or require a new segregated interface. Don't force all implementations to handle features they don't support.

#### **D — Dependency Inversion Principle**
Depend on abstractions, not concretions. High-level modules should not depend on low-level details. A `ContactTracker` should depend on interfaces like `IContactRepository` and `ITimeProvider` injected through the constructor, not directly on `FileLogger` or `DateTime.Now`.

This makes testing easy—inject mock implementations. Without this, you're tightly coupled to concrete implementations that may be slow or have side effects.

**AI Agent Guidance:** Always inject dependencies through constructors. Never use `new` for dependencies inside classes (except for simple DTOs). Create interfaces for external concerns like time, file system, and network.

---

### Testability Requirements (Non-Negotiable)

#### **1. Constructor Injection Only**

All dependencies MUST be injected via constructors. Always null-check parameters with `ArgumentNullException`. This ensures all dependencies are explicit and testable.

**AI Agent Guidance:** ALWAYS use constructor injection for required dependencies. Null-check all parameters.

#### **2. Avoid Static State**

Static mutable state kills testability. Tests interfere with each other when they share static state. Use instance fields or dependency injection instead.

**AI Agent Guidance:** Never create static mutable state. Use instance fields and pass state through constructors.

#### **3. Abstract External Dependencies**

Wrap external resources behind testable interfaces. Create `ITimeProvider` instead of using `DateTime.Now` directly, `IFileSystem` for file operations, and `ISerialPortFactory` for serial port creation. This allows you to inject test doubles.

**AI Agent Guidance:** Always abstract external dependencies (file system, network, time, random) behind interfaces for testability.

#### **4. Pure Functions for Business Logic**

Isolate business logic in pure, stateless functions. A `FrequencyFormatter.FormatMhz(double frequency)` that takes input and returns output is pure—easy to test exhaustively without mocks.

**AI Agent Guidance:** Extract pure business logic into separate classes with static methods. Test comprehensively without test doubles.


---

### Code Reuse Strategies (DRY Principle)

#### **1. Extract Common Patterns to Base Classes**

When multiple implementations share behavior, extract to a base class. A `ScannerBridgeBase` can provide the template for `SendAndReceiveAsync` with shared timeout/retry logic, leaving `SendCommandCoreAsync` for subclasses to implement differently (serial vs. UDP specifics).

**AI Agent Guidance:** Before implementing a new bridge type, check if `ScannerBridgeBase` exists. If 80% of the logic is shared across implementations, create a base class.

#### **2. Composition Over Inheritance for Cross-Cutting Concerns**

Use composition for orthogonal concerns like logging, validation, and caching. The **Decorator Pattern** wraps an `IScannerBridge` with a `LoggingBridgeDecorator` to add logging without modifying the original class.

#### **3. Shared Helper Methods in Static Utilities**

For **pure functions** used across multiple classes, create focused static utility classes like `XmlParserHelpers` with methods like `GetAttributeValue`, `GetAttributeInt`, and `GetAttributeDouble`.

**AI Agent Guidance:** Create utility classes named `{Domain}Helpers` (e.g., `XmlParserHelpers`, `FrequencyHelpers`, `MarkupHelpers`). Never create a generic `Utils` class.

#### **4. Configuration Objects to Reduce Parameter Duplication**

When multiple methods share parameter sets, create configuration objects like `ConnectionConfig` with properties for target, port, timeout, retry count, and retry delay.

**AI Agent Guidance:** Use configuration objects to reduce parameter explosion in method signatures.

---

### Layered Architecture (Strict Boundaries)

```
┌─────────────────────────────────────────┐
│  Presentation Layer                     │  ← Depends on Logic + Models
│  (UI Rendering, Input Handling)         │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│  Logic Layer                            │  ← Depends on Core interfaces
│  (Business Rules, Orchestration)        │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│  Core Layer (Interfaces)                │  ← No dependencies
│  (Contracts, Abstractions)              │
└─────────────────────────────────────────┘
              ↑
┌─────────────────────────────────────────┐
│  Infrastructure Layer                   │  ← Implements Core interfaces
│  (Bridges, Data Access, I/O)            │
└─────────────────────────────────────────┘
```

**Rules:**
- **Core** has ZERO dependencies (only .NET BCL)
- **Logic** depends ONLY on **Core** interfaces
- **Infrastructure** implements **Core** interfaces
- **Presentation** orchestrates **Logic** and **Infrastructure**

**Never:**
- Mix UI rendering with business logic
- Put protocol parsing in presentation code
- Embed Spectre.Console dependencies in logic classes
- Have Infrastructure depend on Presentation

---

### Immutability and Value Objects

Use immutable records for data that represents values rather than entities. A `ContactLogEntry` record with `required` properties and `init`-only setters provides value semantics. Include factory methods for controlled creation (e.g., `FromStatus`).

**AI Agent Guidance:** Prefer records over mutable classes for data transfer objects.

---

### Fail-Fast Philosophy

Detect errors early and provide clear diagnostics:

- Validate inputs at boundaries (e.g., `TryValidateFrequency`)
- Use `try-catch` only where meaningful recovery is possible
- Return sentinel values (`"TIMEOUT"`, `"DISCONNECTED"`) rather than throwing on transient failures
- Null-check constructor parameters with `ArgumentNullException`

---

### Minimal External Dependencies

Justify every external package. Prefer standard library solutions:

**Approved Dependencies:**
- **Spectre.Console** — TUI rendering (no viable alternative)
- **xUnit** + **Moq** — Testing (industry standard)
- **System.IO.Ports** — Serial communication (built-in)

**Rejected Dependencies:**
- ❌ **Autofac/Microsoft.DI** — Manual dependency injection is sufficient for this scale
- ❌ **MediatR** — Simple event handlers don't need CQRS complexity
- ❌ **AutoMapper** — Manual mapping is explicit and testable
- ❌ **Polly** — Custom retry logic is ~10 lines

**AI Agent Guidance:** Before adding a NuGet package, ask: "Can I implement this in <50 lines of code?" If yes, don't add the dependency.

---

## Project Structure & Organization

### Folder Hierarchy

```
SDS200.Cli/
├── Core/                  # Interfaces and contracts
│   ├── IScannerBridge.cs
│   └── IDataReceiver.cs
├── Bridges/               # Protocol implementations
│   ├── SerialScannerBridge.cs
│   ├── UdpScannerBridge.cs
│   ├── SerialDataReceiver.cs
│   └── UdpDataReceiver.cs
├── Logic/                 # Business logic and services
│   ├── UnidenParser.cs
│   ├── ContactTracker.cs
│   ├── KeyboardHandler.cs
│   └── FileLogger.cs
├── Models/                # Data structures
│   ├── ScannerStatus.cs
│   ├── ContactLogEntry.cs
│   └── AppSettings.cs
├── Presentation/          # Spectre.Console rendering
│   ├── MainViewRenderer.cs
│   ├── DebugViewRenderer.cs
│   ├── CommandViewRenderer.cs
│   ├── MenuViewRenderer.cs
│   ├── ConnectionSetupService.cs
│   ├── MarkupConstants.cs
│   └── DebugDisplayFactory.cs
└── Program.cs             # Entry point and orchestration
```

### Naming Conventions

| Type                  | Pattern               | Example                     |
|-----------------------|-----------------------|-----------------------------|
| **Interfaces**        | `I{Noun}`             | `IScannerBridge`            |
| **Services**          | `{Noun}Service`       | `ConnectionSetupService`    |
| **Handlers**          | `{Noun}Handler`       | `KeyboardHandler`           |
| **Renderers**         | `{Noun}ViewRenderer`  | `MainViewRenderer`          |
| **Parsers**           | `{Vendor}Parser`      | `UnidenParser`              |
| **Data Models**       | `{Noun}` (plain)      | `ScannerStatus`             |
| **Constants**         | `{Noun}Constants`     | `MarkupConstants`           |
| **Private Fields**    | `_{camelCase}`        | `_client`, `_dataReceiver`  |
| **Public Properties** | `PascalCase`          | `IsConnected`, `Frequency`  |

---

## Interface Design & Abstraction

### Principles of Good Interface Design

#### **1. Minimal Surface Area**

Interfaces should expose only what's absolutely necessary. `ITimeProvider` needs only `UtcNow`, not local time, timezone info, formatting, parsing, and 20 other methods.

#### **2. Cohesive Contracts**

All members should relate to a single concept. `IConnectable` focuses on connection state. Avoid mixing connection, command sending, UI rendering, and settings persistence in the same interface.

#### **3. Discoverable Contracts**

Method names and parameters should be self-documenting. `GetRecent(int maxCount)`, `GetByFrequency(double frequencyMhz)`, and `RemoveOlderThan(DateTime cutoffTime)` clearly explain what they do.

Avoid unclear methods like `Do(object thing)`, `Get(params object[] args)`, or `Update(int x, int y, string z)`.

#### **4. Async by Default for I/O**

All I/O operations should be asynchronous. Use `ReadAllTextAsync`, `WriteAllTextAsync`, and `FileExistsAsync` instead of blocking calls.

Never create blocking variants like `ReadAllText(string path)` in your I/O interfaces.

---

### Interface Composition Patterns

#### **1. Role Interfaces (ISP)**

Split large interfaces into role-based smaller ones. Instead of one `IScannerBridge` with all functionality, create `ICommandSender` (for commands), `IConnectionManagement` (for connect/disconnect), and `IObservableBridge` (for events). Then compose them into the full `IScannerBridge`.

This lets clients depend on only the roles they need. A `ResponseLogger` only needs `IObservableBridge` for events; a `HealthChecker` only needs `IConnectionManagement`.

**AI Agent Guidance:** When a class needs "some" functionality from an interface, check if there's a smaller role interface it can depend on instead.

#### **2. Generic Interfaces for Reuse**

Create generic interfaces like `IRepository<T>` to avoid duplicating repository patterns. `InMemoryContactRepository` and `InMemorySystemRepository` both implement `IRepository<T>` with their specific entity types.

#### **3. Result Types Over Exceptions**

For expected failures, use result types instead of exceptions. A `ParseResult` record with `Success`, `Status`, and `ErrorMessage` properties allows callers to handle failures gracefully without exception handling.

This separates expected business failures from exceptional programmer errors.


---

### Abstraction Anti-Patterns to Avoid

#### **1. Leaky Abstractions**

Don't expose implementation details. An `IScannerBridge` shouldn't expose `GetUnderlyingPort()` or serial-specific properties like `BaudRate`. Keep the interface transport-agnostic.

#### **2. Header Interfaces**

Don't create interfaces that mirror a single class. Only create an interface if you have multiple implementations or need to mock it for testing. For example, create `ISignalProcessor` if you have `ContactTracker`, `AlertingTracker`, and `StatisticsTracker` all implementing it. But don't create `IContactTracker` if `ContactTracker` is the only implementation.

**AI Agent Guidance:** Only create an interface if:
- Multiple implementations will exist, OR
- You need to mock it for testing, OR
- It's a seam for future extensibility

#### **3. God Interfaces**

Avoid interfaces that do everything. Instead of one `IScannerManager` with 30+ methods handling connection, commands, parsing, UI rendering, contact saving, settings loading, validation, and export—create focused interfaces: `IScannerBridge` (connection + commands), `IResponseParser` (parsing), `IContactRepository` (contact storage), `ISettingsRepository` (settings persistence).

---

### Dependency Injection Patterns

#### **1. Constructor Injection (Primary)**

Always use constructor injection for required dependencies. Null-check all parameters with `ArgumentNullException`.

**AI Agent Guidance:** ALWAYS use constructor injection for required dependencies. Null-check all parameters.

#### **2. Optional Dependencies (Null Object Pattern)**

For optional dependencies, use the Null Object Pattern with a safe default. A `ContactTracker` can accept an optional `ILogger` parameter and use `new NullLogger()` if none is provided.

#### **3. Factory Injection**

Inject factory interfaces when object creation logic needs to vary. A `ConnectionService` depends on `IScannerBridgeFactory` to create bridges, allowing different implementations for testing vs. production.

---

### Common Abstractions Needed

#### **1. Time Abstraction**

Create an `ITimeProvider` interface with `UtcNow` property. Implement `SystemTimeProvider` for production and `FakeTimeProvider` for testing with the ability to set and advance time.

#### **2. File System Abstraction**

Create an `IFileSystem` interface with async methods like `ReadAllTextAsync`, `WriteAllTextAsync`, `FileExistsAsync`, `DeleteFileAsync`, and `GetFilesAsync`. Implement with `SystemFileSystem` for production and `InMemoryFileSystem` for testing.

#### **3. Logging Abstraction**

Create an `ILogger` interface with `LogDebug`, `LogInfo`, `LogWarning`, and `LogError` methods. Implement with `ConsoleLogger` for production and `NullLogger` for testing.

#### **4. Serial Port Abstraction**

Create an `ISerialPort` interface with `IsOpen`, `Open()`, `Close()`, `Write()`, `ReadExisting()`, and `DataReceived` event. Implement with `SystemSerialPort` wrapping the BCL `SerialPort`, and `FakeSerialPort` for testing with command tracking and simulated receives.

---

### When NOT to Abstract

Don't create abstractions for:

1. **Simple DTOs/POCOs** — `ScannerStatus` and `ContactLogEntry` don't need interfaces
2. **Pure static utilities** — `Math.Max()`, `String.Format()` are fine to use directly
3. **Framework types you don't control** — Don't wrap `List<T>`, `Dictionary<K,V>` unless necessary
4. **One-time, startup-only code** — Command-line argument parsing doesn't need DI

Keep things simple. Only abstract where you have concrete reasons (polymorphism, testing, configuration).

**AI Agent Guidance:** Create abstractions for external dependencies, algorithms with multiple implementations, cross-cutting concerns, and testability needs. Don't abstract just for the sake of it.

---

## Code Style & Conventions

### General C# Style

```csharp
// ✅ GOOD: Clear, documented, testable
/// <summary>
/// Tracks signal lock state and manages contact log entries.
/// Extracts contact tracking logic from the main polling loop.
/// </summary>
public class ContactTracker
{
    private readonly Queue<ContactLogEntry> _contactLog;
    private readonly int _maxContactLogSize;

    public ContactTracker(Queue<ContactLogEntry> contactLog, int maxContactLogSize = 30)
    {
        _contactLog = contactLog;
        _maxContactLogSize = maxContactLogSize;
    }

    public void ProcessSignalUpdate(ScannerStatus status)
    {
        // Logic here...
    }
}

// ❌ BAD: No docs, unclear responsibilities, static mutable state
public class Tracker
{
    public static Queue<object> Log = new();
    public void Update(object data) { /* ... */ }
}
```

### XML Documentation

**All public types, methods, and properties MUST have XML docs:**

```csharp
/// <summary>
/// Sends a command to the scanner and waits for a response.
/// </summary>
/// <param name="command">Command string (e.g., "GSI", "MDL").</param>
/// <param name="timeout">Maximum time to wait for response.</param>
/// <returns>Response string, "TIMEOUT", or "DISCONNECTED".</returns>
Task<string> SendAndReceiveAsync(string command, TimeSpan timeout);
```

**Include rationale in comments for non-obvious decisions:**

```csharp
// Use an unconnected UDP client — avoids macOS/Linux platform quirks
// where "connected" UDP sockets filter ReceiveAsync incorrectly.
_client = new UdpClient(0); // Bind to any available local port
```

### Constants and Magic Strings

Centralize UI strings in `MarkupConstants.cs`:

```csharp
// ✅ GOOD: Centralized, reusable, type-safe
public const string KeyPressedD = "D (Toggle Debug View)";
public const string StatusConnected = "CONNECTED";

// In code:
EnqueueDebug(MarkupConstants.KeyPressedD);

// ❌ BAD: Scattered magic strings
EnqueueDebug("D (Toggle Debug View)");
```

---

## Design Patterns

### 1. **Bridge Pattern** (Transport Abstraction)

`IScannerBridge` decouples the application from transport details (Serial vs. UDP). Both implementations provide the same interface without exposing how they work internally.

**When to extend:** Adding Bluetooth, WebSocket, or mock implementations.

**AI Agent Guidance:** Any new transport mechanism should implement `IScannerBridge`. Never add transport-specific logic to the Logic layer.

---

### 2. **Strategy Pattern** (Pluggable Algorithms)

Use strategies for interchangeable algorithms. `IResponseParser` implementations like `GsiXmlParser`, `MdlTextParser` can be plugged into a `ResponseHandler` that selects the right parser based on the response format.

**AI Agent Guidance:** When adding support for new response types, create a new `IResponseParser` implementation rather than extending a monolithic parser class.

---

### 3. **Factory Pattern** (Object Creation)

Centralize creation logic for complex object graphs. An `IScannerBridgeFactory` creates the right bridge type (Serial or UDP) and injects its dependencies.

**AI Agent Guidance:** Use factories when object creation has dependencies, logic may change based on configuration, or you need different implementations for testing.


---

### 4. **Repository Pattern** (Data Access Abstraction)

Abstract data persistence to enable testing and swappable backends. An `IContactRepository` defines storage operations without tying to a specific technology (in-memory vs. file-based).

**AI Agent Guidance:** Create repositories for any data that needs persistence, caching, size limits, or eviction policies, and testing with fake data.


---

### 5. **Decorator Pattern** (Cross-Cutting Concerns)

Add functionality without modifying existing classes. A `LoggingScannerBridge` and `RetryingScannerBridge` wrap an `IScannerBridge` to add logging and retry logic.

**AI Agent Guidance:** Use decorators for logging, retry logic, caching, validation, and performance monitoring. Never implement these concerns directly in core business classes.

---

### 6. **Observer Pattern** (Event-Driven Communication)

Decouple event producers from consumers. Multiple independent observers (`DebugLogger`, `ResponseParser`) can subscribe to `IScannerBridge` events without coupling to each other.

**AI Agent Guidance:** Use events when multiple components need to react to the same occurrence, components should remain decoupled, and you want to avoid circular dependencies.

---

### 7. **Template Method Pattern** (Algorithm Skeleton)

Define algorithm structure in base class, defer steps to subclasses:

```csharp
// ✅ GOOD: Template method for common bridge behavior
public abstract class ScannerBridgeBase : IScannerBridge
{
    // Template method defining the algorithm
    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout)
    {
        ValidateCommand(command);           // Common validation
        var tcs = PrepareResponse();        // Common setup
        await SendCoreAsync(command);       // Subclass-specific
        return await WaitForResponseAsync(tcs, timeout); // Common waiting
    }
    
    protected abstract Task SendCoreAsync(string command);
    
    private void ValidateCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty");
    }
    
    private TaskCompletionSource<string> PrepareResponse()
    {
        var tcs = new TaskCompletionSource<string>();
        lock (_lock) { _responseTcs = tcs; }
        return tcs;
    }
}
```

---

### 8. **Null Object Pattern** (Avoid Null Checks)

Provide default implementations instead of null:

```csharp
// ✅ GOOD: Null object pattern for logger
public interface ILogger
{
    void LogDebug(string message);
    void LogInfo(string message);
    void LogError(string message, Exception? ex = null);
}

public class NullLogger : ILogger
{
    public void LogDebug(string message) { }
    public void LogInfo(string message) { }
    public void LogError(string message, Exception? ex = null) { }
}

// Usage: no null checks needed
public class Service
{
    private readonly ILogger _logger;
    
    public Service(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void DoWork()
    {
        _logger.LogDebug("Working..."); // Always safe to call
    }
}

// Production: var service = new Service(new FileLogger());
// Testing: var service = new Service(new NullLogger());
```

---

### 9. **Builder Pattern** (Complex Object Construction)

Simplify creation of objects with many optional parameters:

```csharp
// ✅ GOOD: Builder for complex configuration
public class ScannerConfigBuilder
{
    private string? _target;
    private int? _port;
    private TimeSpan _timeout = TimeSpan.FromSeconds(5);
    private int _retryCount = 3;
    private bool _enableLogging = false;
    
    public ScannerConfigBuilder WithTarget(string target)
    {
        _target = target;
        return this;
    }
    
    public ScannerConfigBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }
    
    public ScannerConfigBuilder WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }
    
    public ScannerConfig Build()
    {
        if (_target == null || _port == null)
            throw new InvalidOperationException("Target and port required");
            
        return new ScannerConfig
        {
            Target = _target,
            Port = _port.Value,
            Timeout = _timeout,
            RetryCount = _retryCount,
            EnableLogging = _enableLogging
        };
    }
}

// Usage
var config = new ScannerConfigBuilder()
    .WithTarget("192.168.1.100")
    .WithPort(50536)
    .WithTimeout(TimeSpan.FromSeconds(10))
    .Build();
```

---

### 10. **Service Locator** (Explicitly Avoided)

**DO NOT** use a service locator or DI container. Dependencies are:
- Passed via constructors
- Created explicitly in `Program.cs` (composition root)
- Kept minimal and obvious

```csharp
// ❌ BAD: Service locator (hidden dependencies)
public class ContactTracker
{
    public void Process(Contact contact)
    {
        var logger = ServiceLocator.Get<ILogger>();  // Hidden dependency
        var repo = ServiceLocator.Get<IRepository>(); // Can't see what this needs
    }
}

// ✅ GOOD: Explicit constructor injection
public class ContactTracker
{
    private readonly ILogger _logger;
    private readonly IRepository _repository;
    
    public ContactTracker(ILogger logger, IRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
}
```

**Rationale:** For a CLI application of this scale, manual dependency wiring is:
- More transparent
- Easier to understand
- Faster to compile
- Simpler to debug

---

## Threading & Concurrency

### Threading Model

The application uses three concurrent flows:

1. **Main Thread** (UI rendering via `AnsiConsole.Live`)
2. **Keyboard Task** (`Task.Run(() => keyboard.RunAsync(cts.Token))`)
3. **Data Receiver Task** (Serial event handler or UDP receive loop)

### Synchronization Rules

**Thread-Safe Queues:**
```csharp
// Use ConcurrentQueue for cross-thread communication
var keyboardInputLog = new ConcurrentQueue<string>();
var rawRadioData = new ConcurrentQueue<string>();
```

**Lock-Protected State:**
```csharp
private readonly object _lock = new();

public void ExpectResponse(TaskCompletionSource<string> tcs, bool isXmlCommand)
{
    lock (_lock)
    {
        _responseTcs = tcs;
        _expectingXmlResponse = isXmlCommand;
    }
}
```

**Never:**
- Call `Task.Wait()` or `.Result` in async code
- Share mutable state without synchronization
- Use `ConfigureAwait(false)` in application code (only in libraries)

---

## Error Handling

### Transient vs. Fatal Errors

**Transient errors** (network timeouts, busy ports) should return sentinel values without throwing. Return `"TIMEOUT"` or `"DISCONNECTED"` and let the caller decide how to respond.

**Fatal errors** (invalid configuration, missing dependencies) should fail early with clear messages via `AnsiConsole.MarkupLine` and exit gracefully.

**AI Agent Guidance:** Distinguish between expected transient failures (retry) and fatal errors (fail fast).

### Exception Handling

Catch **specific exceptions** at boundaries, not generic `Exception`. For example, catch `OperationCanceledException` during shutdown and `SocketException` for network errors. Log the error, maybe add a brief delay to prevent tight loops, and return sentinel values.

**Never:**
- Swallow exceptions silently (`catch { }`)
- Use `catch (Exception)` unless at top-level boundaries

---

## Testing Philosophy

### Test Coverage Priorities

1. **Domain logic** (parsing, validation, business rules) — **HIGHEST PRIORITY** (aim for 90%+)
2. **Service orchestration** (handlers, trackers) — **HIGH PRIORITY** (aim for 80%+)
3. **Infrastructure integration** (bridge tests, I/O) — **MEDIUM PRIORITY** (60%+)
4. **UI rendering** (visual inspection preferred) — **LOW PRIORITY** (<30% acceptable)

**AI Agent Guidance:** When adding new functionality, write tests FIRST for domain logic, THEN implement. For UI changes, visual inspection is acceptable.

---

### Test-Driven Development (TDD) Process

Follow the RED-GREEN-REFACTOR cycle:
1. **RED:** Write a failing test that clarifies requirements
2. **GREEN:** Make it pass with the simplest code possible
3. **REFACTOR:** Clean up while keeping tests green

**AI Agent Guidance:** For new features, write the test first to clarify requirements. This guides design decisions.

---

### Test Structure (AAA Pattern)

Use Arrange-Act-Assert pattern:
- **Arrange:** Set up test data and dependencies
- **Act:** Call the method under test
- **Assert:** Verify the results with one logical assertion per test

Use clear separators and descriptive variable names. Test names should be: `MethodName_Scenario_ExpectedBehavior`

---

### Test Data Management

#### **Embedded Resources**

Store test data (XML, JSON) in a `GsiTestData.cs` class with const string fields for reuse across multiple test cases.

#### **Test Data Builders**

Create builders like `ScannerStatusBuilder` with fluent methods to construct test objects with sensible defaults and override only what matters for each test.

---

### Test Organization

#### **File Structure**

Organize tests by layer: `Unit/Logic/`, `Unit/Models/`, `Unit/Helpers/`, `Integration/`, and `TestDoubles/`. Keep test data in a `TestData/` folder with shared constants and builders.

#### **Test Naming Conventions**

Use the pattern `MethodName_Scenario_ExpectedBehavior`:
- `ParseGsiResponse_ValidXml_ReturnsSuccess`
- `ProcessSignalUpdate_RssiAboveThreshold_CreatesContact`
- `ProcessSignalUpdate_RssiBelowThreshold_DoesNotCreateContact`

---

### Parameterized Tests (Theory)

[MemberData(nameof(GetScannerStatusTestData))]
public void ParseGsi_ValidXml_ExtractsCorrectMode(string xml, string expectedMode, double expectedFreq)
{
    var status = new ScannerStatus();
    UnidenParser.UpdateStatus(status, xml);

    Assert.Equal(expectedMode, status.VScreen);
    Assert.Equal(expectedFreq, status.Frequency);
}
```

---

### Integration Testing

```csharp
[Fact]
public async Task FullFlow_SendGsi_ParseResponse_UpdateStatus()
{
    // Arrange: Full stack with real parser, fake bridge
    var bridgeMock = new Mock<IScannerBridge>();
    bridgeMock
        .Setup(x => x.SendAndReceiveAsync("GSI,0", It.IsAny<TimeSpan>()))
        .ReturnsAsync(GsiTestData.TrunkScanXml);
        
    var status = new ScannerStatus();
    var parser = new GsiXmlParser();
    var tracker = new ContactTracker(new InMemoryContactRepository(), new FakeTimeProvider());
    
    // Act: Simulate full workflow
    var response = await bridgeMock.Object.SendAndReceiveAsync("GSI,0", TimeSpan.FromSeconds(1));
    var parseResult = parser.Parse(response);
    status.UpdateFrom(parseResult);
    tracker.ProcessSignalUpdate(status);
    
    // Assert: Verify end-to-end behavior
    Assert.Equal("trunk_scan", status.VScreen);
    Assert.Equal("1234", status.TgId);
    Assert.Single(tracker.GetRecentContacts());
}
```

---

### Performance Testing

```csharp
[Fact]
public void ParseGsi_1000Iterations_CompletesUnder100ms()
{
    var xml = GsiTestData.ConventionalScanXml;
    var parser = new GsiXmlParser();
    var status = new ScannerStatus();
    
    var sw = Stopwatch.StartNew();
    
    for (int i = 0; i < 1000; i++)
    {
        parser.Parse(xml);
    }
    
    sw.Stop();
    
    Assert.True(sw.ElapsedMilliseconds < 100, 
        $"Parsing took {sw.ElapsedMilliseconds}ms, expected <100ms");
}
```

---

### Test Maintainability Guidelines

#### **DRY in Tests (But Not Too DRY)**

```csharp
// ✅ GOOD: Extract setup, keep assertions visible
private ScannerStatus CreateTestStatus(double frequency, int rssi)
{
    return new ScannerStatus
    {
        Frequency = frequency,
        Rssi = $"S{rssi}",
        SystemName = "Test System"
    };
}

[Fact]
public void HighSignal_CreatesContact()
{
    var status = CreateTestStatus(frequency: 154.4150, rssi: 5);
    tracker.ProcessSignalUpdate(status);
    Assert.Single(repo.GetRecent(10));
}

// ❌ BAD: Over-abstracted, hard to understand what's being tested
[Theory]
[InlineData(5, true)]
[InlineData(2, false)]
public void SignalProcessing_VariousRssi_ExpectedBehavior(int rssi, bool shouldCreate)
{
    var status = CreateTestStatus(154.4150, rssi);
    ProcessAndAssert(status, shouldCreate);  // What does this do?
}
```

#### **Explicit Over Clever**

```csharp
// ✅ GOOD: Clear and explicit
[Fact]
public void EmptyRepository_GetRecent_ReturnsEmptyList()
{
    var repo = new InMemoryContactRepository();
    var contacts = repo.GetRecent(10);
    Assert.Empty(contacts);
}

// ❌ BAD: Too clever, what's being tested?
[Theory]
[ClassData(typeof(RepositoryTestCases))]
public void Repository_BehavesCorrectly(IRepository repo, Action<IRepository> action, Func<IRepository, bool> assertion)
{
    action(repo);
    Assert.True(assertion(repo));
}
```

---

### Continuous Testing

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ContactTrackerTests"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Watch mode (run tests on file change)
dotnet watch test
```

**AI Agent Guidance:** After making code changes:
1. Run all tests: `dotnet test`
2. If failures, fix immediately before continuing
3. Add new tests for new functionality
4. Aim for 80%+ coverage on Logic layer

---

## Refactoring & Code Reuse Strategies

### When to Refactor

Refactor immediately when you see:

1. **Duplicate code** — Same logic in 3+ places
2. **Long methods** — >30 lines doing multiple things
3. **Large classes** — >300 lines or >10 methods
4. **God objects** — Class that knows too much or does too much
5. **Feature envy** — Method uses another class's data more than its own
6. **Primitive obsession** — Using strings/ints instead of domain types
7. **Switch statements** — On type codes (replace with polymorphism)

**AI Agent Guidance:** Refactor in small steps. Run tests after each step to ensure behavior unchanged.

---

### Refactoring Patterns

#### **1. Extract Method**

Break long methods (>30 lines) into smaller, focused ones. A `ProcessGsiResponse` method doing XML parsing, mode detection, and status update should be split into `IsValidGsiResponse`, `ExtractXmlFromGsiResponse`, `ParseGsiXml`, and `UpdateStatus`.

#### **2. Extract Class**

When a class has too many responsibilities, split it into focused classes. A "god class" handling connection, commands, parsing, contact saving, settings, and UI rendering should be split into `ScannerBridge`, `ResponseParser`, `ContactRepository`, `SettingsRepository`, and `MainViewRenderer`.
{
    public void RenderUI() { }
}
```

#### **3. Replace Conditional with Polymorphism**

```csharp
// ❌ BEFORE: Switch on type
public class ResponseHandler
{
    public void Handle(string response, string type)
    {
        switch (type)
        {
            case "GSI":
                ParseGsi(response);
                break;
            case "MDL":
                ParseMdl(response);
                break;
            case "PSI":
                ParsePsi(response);
                break;
        }
    }
}

// ✅ AFTER: Polymorphism via strategy pattern
public interface IResponseParser
{
    bool CanParse(string response);
    ParseResult Parse(string response);
}

public class GsiParser : IResponseParser
{
    public bool CanParse(string response) => response.Contains("GSI");
    public ParseResult Parse(string response) { /* ... */ }
}

public class MdlParser : IResponseParser
{
    public bool CanParse(string response) => response.StartsWith("MDL,");
    public ParseResult Parse(string response) { /* ... */ }
}

public class ResponseHandler
{
    private readonly IEnumerable<IResponseParser> _parsers;

    public ParseResult Handle(string response)
    {
        return _parsers
            .FirstOrDefault(p => p.CanParse(response))
            ?.Parse(response) ?? ParseResult.Unrecognized;
    }
}
```

#### **4. Introduce Parameter Object**

Replace long parameter lists with configuration objects. A method with 7+ parameters (target, portOrBaud, timeout, retryCount, retryDelay, enableLogging, autoReconnect) should use a `ConnectionConfig` record.

#### **5. Replace Magic Numbers with Named Constants**

Replace hardcoded numbers with named constants. Instead of checking `if (rssi > 3)` and `if (queue.Count > 100)`, use `ContactTrackerConfig.RssiThresholdForContact` and `ContactTrackerConfig.MaxContactQueueSize`.

#### **6. Extract Interface from Concrete Class**

Replace direct dependencies on concrete classes with interfaces. A `ContactTracker` tightly coupled to `FileLogger("/var/log/contacts.txt")` should depend on injected `ILogger` instead.

---

### Code Reuse Techniques

#### **1. Template Method Pattern**

Define algorithm skeleton in base class, defer specific steps to subclasses. `ScannerBridgeBase` provides common `SendAndReceiveAsync` flow (validate, prepare, send, wait, normalize), while `SerialScannerBridge` and `UdpScannerBridge` implement `SendCommandCoreAsync` differently.

#### **2. Helper Classes for Common Operations**

Create focused static utility classes like `XmlParserHelpers` with methods for extracting attributes and elements. Reuse across all parsers.
// Usage across multiple parsers
public class GsiXmlParser
{
    private void ParseTrunkingScan(XElement root, ScannerStatus status)
    {
        var tgidElement = root.Element("TGID");
        status.TgId = XmlParserHelpers.GetAttributeValue(tgidElement, "TGID");
        status.ChannelName = XmlParserHelpers.GetAttributeValue(tgidElement, "Name");
        status.HitCount = XmlParserHelpers.GetAttributeInt(tgidElement, "HitCount");
    }
}
```

#### **3. Extension Methods for Domain-Specific Operations**

```csharp
// ✅ GOOD: Extension methods for common operations
public static class ScannerStatusExtensions
{
    public static bool IsSignalPresent(this ScannerStatus status)
    {
        return !string.IsNullOrEmpty(status.Rssi) && 
               status.Rssi != "S0";
    }
    
    public static bool IsHighQualitySignal(this ScannerStatus status)
    {
        return status.Rssi switch
        {
            "S4" or "S5" => true,
            _ => false
        };
    }
    
    public static string GetDisplayFrequency(this ScannerStatus status)
    {
        return status.Frequency.ToString("F4") + " MHz";
    }
}

// Usage
if (status.IsHighQualitySignal())
{
    contactTracker.CreateContact(status);
}

var display = status.GetDisplayFrequency();  // "154.4150 MHz"
```

#### **4. Composition Over Inheritance**

```csharp
// ✅ GOOD: Compose functionality via decorators
public class LoggingScannerBridge : IScannerBridge
{
    private readonly IScannerBridge _inner;
    private readonly ILogger _logger;
    
    public LoggingScannerBridge(IScannerBridge inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }
    
    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout)
    {
        _logger.LogDebug($"→ {command}");
        var response = await _inner.SendAndReceiveAsync(command, timeout);
        _logger.LogDebug($"← {response}");
        return response;
    }
    
    // Delegate other methods...
}

public class RetryingScannerBridge : IScannerBridge
{
    private readonly IScannerBridge _inner;
    private readonly int _maxRetries;
    
    public async Task<string> SendAndReceiveAsync(string command, TimeSpan timeout)
    {
        for (int i = 0; i < _maxRetries; i++)
        {
            var response = await _inner.SendAndReceiveAsync(command, timeout);
            if (response != "TIMEOUT") return response;
        }
        return "TIMEOUT";
    }
}

// Stack behaviors
var bridge = new UdpScannerBridge();
bridge = new RetryingScannerBridge(bridge, maxRetries: 3);
bridge = new LoggingScannerBridge(bridge, logger);
```

#### **5. Factory Methods for Object Creation**

```csharp
// ✅ GOOD: Factory methods encapsulate creation logic
public record ContactLogEntry
{
    public DateTime LockTime { get; init; }
    public double Frequency { get; init; }
    public string SystemName { get; init; }
    
    // Factory method
    public static ContactLogEntry FromStatus(ScannerStatus status, ITimeProvider timeProvider)
    {
        return new ContactLogEntry
        {
            LockTime = timeProvider.UtcNow,
            Frequency = status.Frequency,
            SystemName = status.SystemName ?? "UNKNOWN"
        };
    }
    
    // Factory method for test data
    public static ContactLogEntry CreateTestContact(double frequency, string systemName)
    {
        return new ContactLogEntry
        {
            LockTime = DateTime.UtcNow,
            Frequency = frequency,
            SystemName = systemName
        };
    }
}
```

---

### Identifying Duplicate Code

Use these techniques to find duplication:

#### **1. Similar Method Bodies**

```csharp
// ❌ DUPLICATION: Almost identical parsing logic
private void ParseConventionalScan(XElement root, ScannerStatus status)
{
    var sys = root.Element("System");
    if (sys != null)
    {
        status.SystemName = sys.Attribute("Name")?.Value ?? "";
        status.SystemType = sys.Attribute("Type")?.Value;
    }
}

private void ParseTrunkingScan(XElement root, ScannerStatus status)
{
    var sys = root.Element("System");
    if (sys != null)
    {
        status.SystemName = sys.Attribute("Name")?.Value ?? "";
        status.SystemType = sys.Attribute("Type")?.Value;
    }
}

// ✅ REFACTORED: Extract common logic
private void ParseSystemInfo(XElement root, ScannerStatus status)
{
    var sys = root.Element("System");
    if (sys != null)
    {
        status.SystemName = sys.Attribute("Name")?.Value ?? "";
        status.SystemType = sys.Attribute("Type")?.Value;
    }
}

private void ParseConventionalScan(XElement root, ScannerStatus status)
{
    ParseSystemInfo(root, status);
    // ... conventional-specific logic
}

private void ParseTrunkingScan(XElement root, ScannerStatus status)
{
    ParseSystemInfo(root, status);
    // ... trunking-specific logic
}
```

#### **2. Copy-Paste Code Smell**

```csharp
// ❌ DUPLICATION: Same null-checking pattern
public void ProcessContact(ContactLogEntry contact)
{
    if (contact == null) throw new ArgumentNullException(nameof(contact));
    if (contact.Frequency <= 0) throw new ArgumentException("Invalid frequency");
    if (string.IsNullOrEmpty(contact.SystemName)) throw new ArgumentException("System name required");
    
    // Process...
}

public void SaveContact(ContactLogEntry contact)
{
    if (contact == null) throw new ArgumentNullException(nameof(contact));
    if (contact.Frequency <= 0) throw new ArgumentException("Invalid frequency");
    if (string.IsNullOrEmpty(contact.SystemName)) throw new ArgumentException("System name required");
    
    // Save...
}

// ✅ REFACTORED: Extract validation
private void ValidateContact(ContactLogEntry contact)
{
    if (contact == null) throw new ArgumentNullException(nameof(contact));
    if (contact.Frequency <= 0) throw new ArgumentException("Invalid frequency");
    if (string.IsNullOrEmpty(contact.SystemName)) throw new ArgumentException("System name required");
}

public void ProcessContact(ContactLogEntry contact)
{
    ValidateContact(contact);
    // Process...
}

public void SaveContact(ContactLogEntry contact)
{
    ValidateContact(contact);
    // Save...
}
```

---

### Refactoring Safety Rules

1. **Tests first** — Ensure good test coverage before refactoring
2. **Small steps** — One transformation at a time
3. **Run tests** — After each small change
4. **Commit often** — So you can roll back if needed
5. **No behavior change** — Refactoring should not change functionality

```bash
# Refactoring workflow
git checkout -b refactor/extract-parser-helpers
dotnet test  # All green before starting
# Make small change (e.g., extract one helper method)
dotnet test  # Verify still green
git commit -m "Extract GetAttributeValue helper"
# Repeat until refactoring complete
dotnet test  # Final verification
```

**AI Agent Guidance:** When refactoring:
1. Ask user if tests exist for the code being refactored
2. If not, write tests first
3. Make one small transformation
4. Run tests
5. Repeat until complete

---

## Documentation Standards

### README Requirements

Maintain the following sections in `README.md`:

1. **Quick Start** — installation and first run
2. **Scanner Configuration** — hardware setup for Serial/UDP
3. **Features** — what the app does
4. **Hotkeys** — keyboard reference
5. **Protocol Documentation** — link to specs in `docs/`
6. **Troubleshooting** — common issues

### Code Comments

**When to comment:**
- **Why**, not **what** (code should be self-documenting)
- Protocol quirks and workarounds
- Performance-sensitive code
- Non-obvious algorithm choices

```csharp
// ✅ GOOD: Explains the rationale
// Multi-packet XML assembly uses Footer No/EOT attributes for sequencing.
// Only GLT commands return multi-packet responses; GSI/PSI are single-packet.

// ❌ BAD: Obvious from the code
// Set the client to null
_client = null;
```

### Changelog

Document breaking changes, new features, and bug fixes in `docs/CHANGELOG.md` (create if needed).

---

## UI/UX Guidelines

### Spectre.Console Conventions

**Markup Safety:**
```csharp
// ✅ GOOD: Escape user input to prevent markup injection
var safeData = Markup.Escape($"[{DateTime.Now:HH:mm:ss}] {data.Trim()}");

// ❌ BAD: Raw input can crash the UI with unbalanced brackets
debugLog.Enqueue($"[{DateTime.Now:HH:mm:ss}] {data}");
```

**Color Palette:**

| Element          | Color      | Example                              |
|------------------|------------|--------------------------------------|
| Frequency        | Green      | `[green]154.4150 MHz[/]`             |
| System Info      | Blue       | `[blue]SYS:[/]`                      |
| TGID/Channel     | Yellow     | `[yellow]TGID:[/]`                   |
| Warnings         | Yellow     | `[yellow]Timeout[/]`                 |
| Errors           | Red        | `[red]DISCONNECTED[/]`               |
| Success/Active   | Bold Green | `[bold green]CONNECTED[/]`           |
| Metadata         | Grey/Dim   | `[grey]UID:[/]`, `[dim]VOL 5[/]`     |

**Layout Hierarchy:**

```
┌─ Hero Panel (Frequency + Mode) ──────────┐
│  Large Figlet text, centered             │
├─ Mid Panel (Identity + RSSI) ────────────┤
│  Left: System/Dept/Channel table         │
│  Right: RSSI bar + signal strength       │
├─ Contacts Log (Recent contacts) ─────────┤
│  Time, Freq, System, Duration            │
├─ Hotkeys (Context-aware help) ───────────┤
│  Compact or expanded based on SPACE      │
└─ Footer (Connection + Status) ───────────┘
```

### Keyboard Interaction

- **No blocking input** — use `Console.KeyAvailable` polling
- **Single-key actions** — no multi-key combos
- **SPACE to reveal** — expanded hotkey help on hold
- **ESC to exit** — mode-specific back navigation
- **Case-insensitive** — `M` and `m` both work

---

## Extension Points

### Adding a New Transport

1. Create `{Transport}ScannerBridge : IScannerBridge` in `Bridges/`
2. Create `{Transport}DataReceiver : IDataReceiver` if needed
3. Update `ConnectionSetupService` to detect and configure
4. Add tests in `SDS200.Cli.Tests/`

**Example:** Bluetooth via RFCOMM

```csharp
public class BluetoothScannerBridge : IScannerBridge
{
    // Implement interface...
}
```

### Adding a New View Mode

1. Add enum value to `ViewMode.cs`
2. Create `{Mode}ViewRenderer.cs` in `Presentation/`
3. Update `RenderView()` dispatcher in `Program.cs`
4. Add hotkey in `KeyboardHandler.cs`

### Adding Audio Streaming (RTSP)

**Recommended approach:**

1. Create `RtspAudioPlayer.cs` in `Logic/`
2. Use `LibVLCSharp` for cross-platform RTSP support
3. Wire into `UdpScannerBridge` with `StartAudioAsync()` / `StopAudioAsync()`
4. Add `A` key toggle in `KeyboardHandler`
5. Add 🔊 indicator in `MainViewRenderer`

**Design principles:**
- Audio is **separate from command flow** (different port, lifecycle)
- Only available in **UDP mode** (network-only feature)
- Graceful degradation if VLC libraries unavailable

---

## Appendix: Protocol Notes

### Uniden SDS200 Protocol Quirks

1. **Command Terminator:** All commands end with `\r` (carriage return)
2. **UDP Packet Assembly:** Only `GLT` uses multi-packet with `<Footer No="X" EOT="1"/>`
3. **XML Inconsistencies:** Some attributes use `"None"` (string), others use `"0"` (numeric) for the same concept
4. **Case Sensitivity:** Commands are case-insensitive, but normalize to uppercase for consistency
5. **Timeout Handling:** 500ms is typical, but multi-packet GLT may need 5-10s

### Serial Port Filtering (macOS)

Exclude Bluetooth and debug interfaces:

```csharp
ports.Where(p => !p.Contains("Bluetooth") && !p.Contains("debug"))
```

### UDP Platform Quirks

**macOS/Linux:** "Connected" UDP sockets may filter `ReceiveAsync` incorrectly. Use unconnected `UdpClient(0)` and `SendAsync` with explicit endpoint.

---

## Quick Reference Checklist

When adding new code, ask:

- [ ] Is this in the correct layer? (Core / Bridge / Logic / Presentation)
- [ ] Does it have XML documentation?
- [ ] Are magic strings moved to `MarkupConstants`?
- [ ] Is user input escaped before Spectre.Console markup?
- [ ] Are exceptions caught at appropriate boundaries?
- [ ] Is concurrency handled with thread-safe primitives?
- [ ] Does it follow the naming conventions?
- [ ] Can it be unit tested without mocking the entire world?
- [ ] Is the protocol behavior documented with a comment?

---

## AI Agent Decision Trees

### Decision Tree: Where Does This Code Belong?

```
User asks to add functionality...
│
├─ Is it about UI rendering/display?
│  └─ YES → Presentation/{Feature}ViewRenderer.cs
│
├─ Is it about transport/communication (Serial/UDP/Bluetooth)?
│  └─ YES → Bridges/{Transport}ScannerBridge.cs (implements IScannerBridge)
│
├─ Is it a contract/interface definition?
│  └─ YES → Core/I{Name}.cs
│
├─ Is it business logic/orchestration?
│  └─ YES → Logic/{Feature}Handler.cs or Logic/{Feature}Service.cs
│
├─ Is it a data structure/model?
│  └─ YES → Models/{Name}.cs (prefer immutable records)
│
└─ Is it a pure utility function?
   └─ YES → Logic/{Domain}Helpers.cs (static class)
```

### Decision Tree: Should I Create an Interface?

```
I'm implementing a new class...
│
├─ Will there be multiple implementations?
│  └─ YES → Create interface in Core/
│
├─ Do I need to mock this for testing?
│  └─ YES → Create interface in Core/
│
├─ Is this an external dependency (file system, network, time)?
│  └─ YES → Create interface in Core/
│
├─ Is this for future extensibility (plugins, strategies)?
│  └─ YES → Create interface in Core/
│
└─ NO → Just implement the class, no interface needed
```

### Decision Tree: How Should I Handle Errors?

```
An error occurred in my code...
│
├─ Is this a programming error (null parameter, invalid state)?
│  └─ YES → Throw exception immediately (fail-fast)
│           Example: throw new ArgumentNullException(nameof(param))
│
├─ Is this an expected business condition (timeout, not found)?
│  └─ YES → Return sentinel value or Result<T>
│           Example: return "TIMEOUT" or ParseResult.Fail("reason")
│
├─ Is this a transient infrastructure failure (network blip)?
│  └─ YES → Log, return sentinel, let caller retry
│           Example: catch (SocketException) { return "DISCONNECTED"; }
│
└─ Is this a fatal unrecoverable error (out of memory)?
   └─ YES → Log and terminate gracefully
            Example: AnsiConsole.MarkupLine("[red]Fatal error[/]"); Environment.Exit(1);
```

### Decision Tree: Should I Extract This Code?

```
I'm looking at a piece of code...
│
├─ Is this code duplicated in 3+ places?
│  └─ YES → Extract to helper method or base class
│
├─ Is this method longer than 30 lines?
│  └─ YES → Consider extracting smaller focused methods
│
├─ Does this method have multiple responsibilities?
│  └─ YES → Split into multiple methods following SRP
│
├─ Does this code mix layers (e.g., UI + business logic)?
│  └─ YES → Separate concerns into appropriate layers
│
└─ Is this code hard to test due to dependencies?
   └─ YES → Extract interface and inject dependency
```

---

## AI Agent Implementation Workflow

### Workflow: Adding a New Feature

```
1. UNDERSTAND THE REQUEST
   ├─ What layer does this belong in?
   ├─ What existing code can be reused?
   ├─ What interfaces/abstractions exist?
   └─ Are there similar features to reference?

2. DESIGN THE SOLUTION
   ├─ Identify classes/interfaces needed
   ├─ Draw out dependencies
   ├─ Plan test strategy
   └─ Check SOLID principles compliance

3. WRITE TESTS FIRST (TDD)
   ├─ Write failing test that demonstrates bug
   ├─ Run: dotnet test (should fail)
   └─ Proceed to implementation

4. IMPLEMENT INCREMENTALLY
   ├─ Create interfaces in Core/ (if needed)
   ├─ Implement classes in appropriate layer
   ├─ Add XML documentation
   ├─ Extract constants to MarkupConstants
   └─ Run: dotnet test after each small change

5. REFACTOR
   ├─ Look for duplication
   ├─ Extract helper methods
   ├─ Simplify conditionals
   └─ Run: dotnet test (should still pass)

6. VALIDATE
   ├─ Run: dotnet test
   ├─ Check: get_errors for any issues
   ├─ Verify layer boundaries respected
   └─ Ensure XML documentation complete
```

### Workflow: Fixing a Bug

```
1. REPRODUCE THE BUG
   ├─ Write failing test that demonstrates bug
   ├─ Run: dotnet test
   └─ Verify test fails as expected

2. LOCATE THE ROOT CAUSE
   ├─ Use stack trace / error messages
   ├─ Check logs / debug output
   └─ Identify which class/method is responsible

3. FIX MINIMALLY
   ├─ Make smallest change to fix issue
   ├─ Don't refactor while fixing
   └─ Run: dotnet test

4. VERIFY FIX
   ├─ Test should now pass
   ├─ All other tests still pass
   └─ Manual verification if needed

5. ADD DEFENSIVE CODE
   ├─ Add validation to prevent recurrence
   ├─ Add XML comments explaining fix
   └─ Consider edge cases
```

### Workflow: Refactoring Existing Code

```
1. ENSURE TEST COVERAGE
   ├─ Check existing tests for the code
   ├─ If <60% coverage, write tests first
   └─ Run: dotnet test (all green)

2. IDENTIFY CODE SMELLS
   ├─ Duplication
   ├─ Long methods
   ├─ God classes
   ├─ Feature envy
   └─ Magic numbers/strings

3. PLAN SMALL STEPS
   ├─ Extract method
   ├─ Extract class
   ├─ Introduce interface
   └─ Replace conditional with polymorphism

4. REFACTOR INCREMENTALLY
   ├─ Make ONE small transformation
   ├─ Run: dotnet test
   ├─ Commit if green: git commit -m "..."
   └─ Repeat until complete

5. FINAL VALIDATION
   ├─ Run: dotnet test
   ├─ Check: get_errors
   └─ Review code for clarity
```

---

## AI Agent Communication Guidelines

### When Proposing Changes to User

**DO:**
- ✅ Explain WHAT you're changing
- ✅ Explain WHY it follows design principles
- ✅ Show BEFORE/AFTER code snippets for clarity
- ✅ List files that will be created/modified
- ✅ Mention tests you'll add

**DON'T:**
- ❌ Ask for permission for every small decision
- ❌ Propose changes without justification
- ❌ Show code without context
- ❌ Make changes without running tests

### Example Good Proposal

```
I'll add a new IResponseParser implementation for PSI commands.

WHY: This follows the Open/Closed Principle - we extend behavior 
without modifying existing parser code.

FILES TO CREATE:
- SDS200.Cli/Logic/PsiXmlParser.cs (new parser implementation)
- SDS200.Cli.Tests/Unit/Logic/PsiXmlParserTests.cs (unit tests)

FILES TO MODIFY:
- SDS200.Cli/Logic/ResponseHandler.cs (register new parser)

TESTS: I'll add 5 test cases covering valid PSI XML, malformed XML, 
and edge cases.

Proceeding with implementation...
```

### When Uncertain

**DO ASK** if:
- Multiple valid design approaches exist
- Breaking change required
- External dependency needed
- Architecture-level decision required

**DON'T ASK** if:
- Naming a variable
- Choosing between similar helper methods
- Formatting code
- Adding XML documentation

---

## AI Agent Best Practices

### Code Generation

1. **Always include XML documentation**
   ```csharp
   /// <summary>
   /// Parses a GSI XML response and updates scanner status.
   /// </summary>
   /// <param name="xml">XML response from GSI,0 command.</param>
   /// <returns>True if parsing succeeded, false otherwise.</returns>
   public bool Parse(string xml) { }
   ```

2. **Always validate parameters**
   ```csharp
   public ContactTracker(IContactRepository repository)
   {
       _repository = repository ?? throw new ArgumentNullException(nameof(repository));
   }
   ```

3. **Always use const/readonly when possible**
   ```csharp
   public const int DefaultPort = 50536;
   private readonly ILogger _logger;
   ```

4. **Always escape user input for UI**
   ```csharp
   var escaped = Markup.Escape(status.SystemName);
   AnsiConsole.MarkupLine($"[blue]{escaped}[/]");
   ```

### Test Generation

1. **Follow AAA pattern**
   ```csharp
   [Fact]
   public void Method_Scenario_Expected()
   {
       // Arrange
       var sut = new ClassUnderTest(dependencies);
       
       // Act
       var result = sut.Method(input);
       
       // Assert
       Assert.Equal(expected, result);
   }
   ```

2. **Use descriptive test names**
   ```csharp
   // ✅ GOOD
   ParseGsiResponse_ValidXml_ReturnsSuccess
   ProcessSignalUpdate_RssiAboveThreshold_CreatesContact
   
   // ❌ BAD
   Test1
   TestParser
   ```

3. **Create test data classes**

Store test constants in a `GsiTestData` class with const string fields for reusable test data.

### Error Handling

1. **Fail fast on programming errors** — Throw `ArgumentNullException` for null parameters
2. **Return sentinels for expected failures** — Return `"TIMEOUT"` or `"DISCONNECTED"` for transient issues
3. **Log and gracefully degrade for infrastructure failures** — Catch `IOException` and continue without the resource

---

## AI Agent Anti-Patterns to Avoid

### DON'T: Create God Classes

Avoid classes that handle multiple concerns. Instead of one class handling connection, parsing, UI rendering, contact saving, and logging, create focused classes: `ScannerBridge`, `ResponseParser`, `MainViewRenderer`, `ContactRepository`, and `Logger`.

### DON'T: Use Static Mutable State

Don't create static mutable state that's shared across tests. Use instance fields instead. A `ScannerStatusManager` should have a private instance field `_currentStatus`, not a static `GlobalState.CurrentStatus`.

### DON'T: Mix Layers

Never put business logic in UI renderers. A `MainViewRenderer` should only handle presentation. Put XML parsing in the Logic layer (`GsiXmlParser`), not in the renderer.

### DON'T: Return Null for Collections

Return empty collections instead of null. `GetContacts()` should return `_contacts.AsReadOnly()` (never null), not `return null` when empty.

### DON'T: Use Magic Strings/Numbers

Use named constants instead of hardcoded values. Instead of `if (rssi > 3)`, use `ContactConfig.RssiThreshold`.

---

## Final Checklist for AI Agents

Before completing a task, verify:

### Code Quality
- [ ] SOLID principles followed
- [ ] All public members have XML documentation
- [ ] No magic strings/numbers
- [ ] Naming conventions followed
- [ ] User input escaped before markup
- [ ] Layer boundaries respected
- [ ] No static mutable state

### Testing
- [ ] Unit tests written/updated
- [ ] Tests use AAA pattern
- [ ] Test names descriptive
- [ ] All tests passing: `dotnet test`
- [ ] No compiler errors: check `get_errors`

### Architecture
- [ ] Interfaces in Core/ (if needed)
- [ ] Implementation in correct layer
- [ ] Dependencies injected via constructor
- [ ] External dependencies abstracted
- [ ] Code reuse via helpers/base classes

### Documentation
- [ ] XML documentation on public APIs
- [ ] Code comments explain WHY, not WHAT
- [ ] Protocol quirks documented
- [ ] README updated (if user-facing change)

When in doubt, prioritize:
1. **Testability** over brevity
2. **Clarity** over cleverness
3. **Simplicity** over premature optimization
4. **Explicitness** over magic

---

**Remember:** This is a scanner control application, not enterprise software. Keep it focused, testable, and maintainable. When faced with a choice between a complex "enterprise" pattern and a simple, explicit solution, choose simple.

---

**End of Design Guide**  
*For questions or clarifications, see existing code examples or open a discussion.*
