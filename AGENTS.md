# Cyclops States — Agent Summary

## What This Package Does

Cyclops States is a **Unity state machine library** that provides a flexible FSM (Finite State Machine) with push-down automata (PDA) capabilities for game state management. It replaces clunky MonoBehaviour-based state patterns with clean, composable, async-aware states.

## Core Architecture

```
CyclopsStateMachine
    └── manages a LinkedList stack of CyclopsBaseState instances
         └── CyclopsState (concrete, uses Action delegates)
         └── Custom subclasses (override virtual methods)
```

### Key Files

| File | Purpose |
|------|---------|
| `CyclopsStateMachine.cs` | Orchestrates the state stack; calls `Update()` on states |
| `CyclopsBaseState.cs` | Abstract base with transitions, lifecycle hooks, async helpers |
| `CyclopsState.cs` | Lightweight concrete state using `Action` properties |
| `CyclopsStateTransition.cs` | Data struct: `Condition`, `Target`, `Op` |
| `StackOp.cs` | Enum: `Replace`, `Push`, `Pop` |

## Transition Types

States support three stack operations:

| Operation | What It Does |
|-----------|--------------|
| **Replace** | Pop current state, push target (default FSM behavior) |
| **Push** | Keep current state, push target on top (PDA behavior) |
| **Pop** | Remove current state, resume state below |

### Transition API Patterns

Each operation has matching overloads:

```csharp
// Predicate-based (condition checked each frame)
AddTransition(target, () => condition);
AddPushTransition(target, () => condition);
AddPopTransition(() => condition);

// Delegate-based (fires on Action invocation)
AddTransition(target, ref myAction);
AddPushTransition(target, ref myAction);
AddPopTransition(ref myAction);

// Generic variants exist for Action<T> through Action<T1,T2,T3,T4>
```

## State Lifecycle

```
Start() → OnEnter() → [OnUpdate() | OnBackgroundUpdate()] → OnExit()
                              ↑
               OnEnterBackgroundMode() / OnExitBackgroundMode()
```

- **Foreground**: Top of stack, receives `OnUpdate()`
- **Background**: Below top, receives `OnBackgroundUpdate()`
- States track `IsActive`, `IsForegroundState`, `IsStopping`

## Async Integration

Each state has an `ExitCancellationToken` that auto-cancels when the state exits:

```csharp
await state.WaitForSecondsAsync(1f);   // Auto-cancelled on exit
await state.NextFrameAsync();
await state.EndOfFrameAsync();
await state.FixedUpdateAsync();
await state.FromAsyncOperation(op);
```

This enables fire-and-forget async patterns tied to state lifetime.

## Predictable Unwinding (Structured Concurrency)

The state stack provides **deterministic cleanup even when states fail or are forcibly stopped**:

```csharp
// ForceStop() guarantees ordered teardown
while (_stateLinkedStack.Count != 0)
{
    TopState.StopImmediately();      // Cancel token + OnExit()
    _stateLinkedStack.RemoveLast();  // Pop from stack
}
```

**Key guarantees:**

1. **Ordered teardown** — States exit top-to-bottom; no state is skipped
2. **Async operations can't outlive their state** — `ExitCancellationToken` kills them immediately
3. **`OnExit()` always runs** — Cleanup logic executes even during forced unwinding
4. **Silent cancellation** — Async helpers catch `OperationCanceledException` internally; cancelled operations don't throw

This is similar to:
- **C++ RAII** — Destructors run during stack unwinding, even on exception
- **Kotlin coroutine scopes / Swift task groups** — Child tasks cancel when parent scope exits
- **Rust's Drop trait** — Guaranteed cleanup regardless of how scope exits

Unlike Unity coroutines (which can leave dangling operations when stopped), the cancellation token pattern ensures no async work survives its state. The lifetime hierarchy is enforced, and teardown is predictable regardless of what each state was doing when unwinding begins.

## Common Patterns

### Basic FSM
```csharp
var fsm = new CyclopsStateMachine();
var gameplay = new CyclopsState { Entered = () => Debug.Log("Play!") };
var gameOver = new CyclopsState { Entered = () => Debug.Log("Game Over") };

gameplay.AddTransition(gameOver, () => playerDead);
fsm.PushState(gameplay);

// In update loop:
fsm.Update();
```

### Modal Overlay (Push/Pop)
```csharp
var hud = new CyclopsState();
var pauseMenu = new CyclopsState();

hud.AddPushTransition(pauseMenu, () => Input.GetKeyDown(KeyCode.Escape));
pauseMenu.AddPopTransition(() => Input.GetKeyDown(KeyCode.Escape));
```

### Exit Transition
```csharp
state.AddExitTransition(nextState); // Triggers when state.Stop() is called
```

## Best Practice: Bootstrap-Only Access

**Fully configure the state machine at bootstrap, then never expose it again.**

The library is designed around this principle:
- States have no reference to their host machine — they can only signal transitions via pre-declared conditions
- States call `Stop()` to request exit, but don't control what happens next (exit transitions do)
- The machine's `Update()` loop evaluates all transitions — states don't imperatively push other states

**Why this matters:**
- **Predictability**: The entire state graph is known upfront and can be reasoned about
- **Testability**: You can verify reachable states from transitions alone
- **Decoupling**: States don't know about the machine or each other — only their own lifecycle

If you pass the state machine into states, they can call `PushState()` directly, bypassing the transition system. This breaks encapsulation and makes the state graph unpredictable.

```csharp
// Bootstrap: wire everything, then only call Update()
var fsm = new CyclopsStateMachine();
var stateA = new CyclopsState();
var stateB = new CyclopsState();
stateA.AddTransition(stateB, () => someCondition);
fsm.PushState(stateA);

// Runtime: just tick — never touch fsm directly again
while (running) fsm.Update();
```

## Implementation Notes

1. **State reuse**: States can be re-entered after exiting (new `CancellationTokenSource` created)
2. **Transition pooling**: Transitions list uses `ListPool<T>` — call `Dispose()` when done
3. **Force stop**: `CyclopsStateMachine.ForceStop()` immediately stops all states in order
4. **No transition removal**: By design, transitions cannot be removed once added

## Testing

Unit tests in `Tests/StateTests.cs` cover:
- Push transition mechanics
- Exit-on-action behavior
- Lifecycle callback counts (enter, update, background, exit)

## Dependencies

- Unity 2023.3+ / Unity 6000
- Uses: `UnityEngine.Pool`, `UnityEngine.Awaitable`, `System.Threading.CancellationToken`

