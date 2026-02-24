# AI Agent Quick Reference Guide

> **Purpose**: Fast lookup for common development decisions when working on SDS200v2

---

## 🎯 Where Does New Code Go?

```
UI/Display           → Presentation/{Feature}ViewRenderer.cs
Transport/Network    → Bridges/{Transport}ScannerBridge.cs
Interface/Contract   → Core/I{Name}.cs
Business Logic       → Logic/{Feature}Handler.cs
Data Model           → Models/{Name}.cs (immutable record)
Pure Utilities       → Logic/{Domain}Helpers.cs (static)
```

---

## 🧪 Test Strategy Quick Reference

| Code Type | Test Location | Coverage Target |
|-----------|---------------|-----------------|
| Domain Logic (parsing, validation) | `Unit/Logic/` | 90%+ |
| Services (handlers, trackers) | `Unit/Logic/` | 80%+ |
| Infrastructure (bridges, I/O) | `Integration/` | 60%+ |
| UI Rendering | Visual inspection | <30% |

---

## ✅ SOLID Principles Checklist

- **S**ingle Responsibility: One class = one reason to change
- **O**pen/Closed: Extend via new classes, not modifying existing
- **L**iskov Substitution: All implementations honor interface contracts
- **I**nterface Segregation: Small, focused interfaces (not fat ones)
- **D**ependency Inversion: Depend on abstractions, inject via constructor

---

## 🔧 When to Create an Interface?

**YES** if:
- Multiple implementations will exist
- Need to mock for testing
- External dependency (file, network, time)
- Future extensibility needed

**NO** if:
- Only one implementation
- Simple POCO/DTO
- Pure utility function

---

## ⚠️ Error Handling Decision

| Error Type | Action | Example |
|------------|--------|---------|
| Programming error | Throw exception | `throw new ArgumentNullException(nameof(param))` |
| Expected failure | Return sentinel | `return "TIMEOUT"` or `ParseResult.Fail()` |
| Transient failure | Log & return | `catch (SocketException) { return "DISCONNECTED"; }` |
| Fatal error | Log & exit | `Environment.Exit(1)` |

---

## 📝 Code Patterns

### Constructor Injection (Always)
```csharp
public class MyService
{
    private readonly IDependency _dependency;
    
    public MyService(IDependency dependency)
    {
        _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
    }
}
```

### XML Documentation (Always)
```csharp
/// <summary>
/// Parses GSI XML response and updates scanner status.
/// </summary>
/// <param name="xml">XML response from GSI,0 command.</param>
/// <returns>True if parsing succeeded, false otherwise.</returns>
public bool Parse(string xml) { }
```

### Test Structure (AAA)
```csharp
[Fact]
public void Method_Scenario_Expected()
{
    // Arrange
    var sut = new ClassUnderTest(mocks);
    
    // Act
    var result = sut.Method(input);
    
    // Assert
    Assert.Equal(expected, result);
}
```

### Immutable Records
```csharp
public record ContactLogEntry
{
    public required DateTime LockTime { get; init; }
    public required double Frequency { get; init; }
    
    public static ContactLogEntry FromStatus(ScannerStatus status, ITimeProvider time)
    {
        return new ContactLogEntry { /* ... */ };
    }
}
```

---

## 🚫 Anti-Patterns to Avoid

| Anti-Pattern | Why Bad | Fix |
|--------------|---------|-----|
| God Class | Too many responsibilities | Extract focused classes |
| Static State | Kills testability | Use instance fields |
| Mixed Layers | Tight coupling | Respect layer boundaries |
| Null Returns | Forces null checks everywhere | Return empty collections |
| Magic Strings | Unclear meaning | Extract to constants |
| Fat Interface | Forces unused methods | Split into role interfaces |

---

## 🔄 Refactoring Triggers

Refactor immediately when:
- [ ] Code duplicated in 3+ places
- [ ] Method >30 lines
- [ ] Class >300 lines or >10 methods
- [ ] Class knows too much about others
- [ ] Using primitives instead of domain types
- [ ] Switch/if-else on type codes

---

## 📦 Common Abstractions

```csharp
// Time (for testability)
public interface ITimeProvider
{
    DateTime UtcNow { get; }
}

// File System (for testability)
public interface IFileSystem
{
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string content);
}

// Logging (for flexibility)
public interface ILogger
{
    void LogDebug(string message);
    void LogError(string message, Exception? ex = null);
}
```

---

## 🎨 Design Patterns Quick Reference

| Pattern | When to Use | Location |
|---------|-------------|----------|
| Bridge | Multiple transports | `IScannerBridge` |
| Strategy | Pluggable algorithms | `IResponseParser` |
| Factory | Complex creation | `IScannerBridgeFactory` |
| Repository | Data access | `IContactRepository` |
| Decorator | Cross-cutting concerns | Logging, retry wrappers |
| Observer | Event-driven | `OnDataReceived` events |
| Template Method | Shared algorithm | `ScannerBridgeBase` |
| Null Object | Avoid null checks | `NullLogger` |

---

## ⚡ Quick Commands

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~MyTests"

# Watch mode (auto-run on changes)
dotnet watch test

# Build
dotnet build

# Check for errors
# (use get_errors tool in copilot)
```

---

## 📋 Pre-Commit Checklist

Before completing any task:

- [ ] SOLID principles followed
- [ ] All public members have XML docs
- [ ] No magic strings/numbers
- [ ] User input escaped before markup
- [ ] Tests written/updated
- [ ] All tests passing: `dotnet test`
- [ ] No compiler errors
- [ ] Dependencies injected via constructor
- [ ] Layer boundaries respected

---

## 🎯 Implementation Workflow

```
1. UNDERSTAND → Identify layer, reuse opportunities
2. DESIGN → Classes, interfaces, dependencies
3. TEST → Write failing test (TDD)
4. IMPLEMENT → Incremental, run tests frequently
5. REFACTOR → Extract duplication, simplify
6. VALIDATE → dotnet test, get_errors
```

---

## 💡 Key Principles

**Prioritize:**
1. **Testability** over brevity
2. **Clarity** over cleverness
3. **Simplicity** over premature optimization
4. **Explicitness** over magic

**Remember:** This is a scanner control app, not enterprise software. Keep it focused, testable, and maintainable.

---

## 📚 Full Documentation

For detailed guidance, see:
- **DESIGN_GUIDE.md** - Complete architectural guide
- **SDSSerialAndUDPProtocol.md** - Protocol specifications
- **DESIGN_GUIDE_IMPROVEMENTS.md** - Recent enhancements

---

**Quick Tip:** When uncertain, consult the Decision Trees in DESIGN_GUIDE.md sections:
- Where Does This Code Belong?
- Should I Create an Interface?
- How Should I Handle Errors?
- Should I Extract This Code?
- What Type of Test Should I Write?

