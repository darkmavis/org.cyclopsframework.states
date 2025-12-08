# Cyclops States — Agent Summary

## What This Package Does

Cyclops States is a **Unity state machine library** that provides a flexible FSM (Finite State Machine) with push-down automata (PDA) capabilities and behavior tree (BT) support for game state management. It uses composition over inheritance — a single sealed `CyclopsState` class handles all use cases.

## Core Architecture

```
CyclopsStateMachine
    └── manages a LinkedList stack of CyclopsState instances
         └── CyclopsState (sealed, uses Action delegates)
         └── CyclopsStateExtensions (async helpers, delegate transitions)
```

### Key Files

| File | Purpose |
|------|---------|
| `CyclopsStateMachine.cs` | Orchestrates the state stack; calls `Update()` on states |
| `CyclopsState.cs` | Sealed state class with lifecycle hooks and BT support |
| `CyclopsStateExtensions.cs` | Extension methods for async helpers and delegate-based transitions |
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
state.AddTransition(target, () => condition);
state.AddPushTransition(target, () => condition);
state.AddPopTransition(() => condition);

// Delegate-based (fires on Action invocation) - extension methods
state.AddTransition(target, ref myAction);
state.AddPushTransition(target, ref myAction);
state.AddPopTransition(ref myAction);

// Generic variants exist for Action<T> through Action<T1,T2,T3,T4>
```

## Behavior Tree Support

States support success/failure reporting for BT-style usage:

```csharp
public enum BtResult { Running, Success, Failure }

// In state update logic:
state.OnUpdate = () =>
{
    if (targetFound) state.Succeed();
    else if (timeout) state.Fail();
};

// BT-aware transitions:
state.OnSuccess(nextState);
state.OnFailure(fallbackState);
state.OnComplete(anyResultState);
```

## State Lifecycle

```
Start() → OnEnter → [OnUpdate | OnBackgroundUpdate] → OnExit
                              ↑
               OnEnterBackground / OnExitBackground
```

- **Foreground**: Top of stack, receives `OnUpdate`
- **Background**: Below top, receives `OnBackgroundUpdate`
- States track `IsActive`, `IsForegroundState`, `Result`

### Lifecycle Hooks

```csharp
var state = new CyclopsState
{
    Name = "GamePlay",              // Optional debug name
    OnEnter = () => { },            // Called when state starts
    OnExit = () => { },             // Called when state stops
    OnUpdate = () => { },           // Called each frame (foreground)
    OnBackgroundUpdate = () => { }, // Called each frame (background)
    OnEnterBackground = () => { },  // Called when pushed to background
    OnExitBackground = () => { }    // Called when returned to foreground
};
```

## Async Integration

Extension methods provide async helpers tied to state lifetime:

```csharp
await state.WaitForSecondsAsync(1f);   // Auto-cancelled on exit
await state.NextFrameAsync();
await state.EndOfFrameAsync();
await state.FixedUpdateAsync();
await state.FromAsyncOperation(op);
```

Each state has an `ExitCancellationToken` that auto-cancels when the state exits.

## Predictable Unwinding (Structured Concurrency)

The state stack provides **deterministic cleanup even when states fail or are forcibly stopped**:

```csharp
// ForceStop() guarantees ordered teardown
while (_stateLinkedStack.Count != 0)
{
    TopState.StopImmediately();      // Cancel token + OnExit
    _stateLinkedStack.RemoveLast();  // Pop from stack
}
```

**Key guarantees:**

1. **Ordered teardown** — States exit top-to-bottom; no state is skipped
2. **Async operations can't outlive their state** — `ExitCancellationToken` kills them immediately
3. **`OnExit` always runs** — Cleanup logic executes even during forced unwinding
4. **Silent cancellation** — Async helpers catch `OperationCanceledException` internally

## Common Patterns

### Basic FSM

```csharp
var fsm = new CyclopsStateMachine();
var gameplay = new CyclopsState { OnEnter = () => Debug.Log("Play!") };
var gameOver = new CyclopsState { OnEnter = () => Debug.Log("Game Over") };

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

### Behavior Tree Node

```csharp
var findTarget = new CyclopsState
{
    Name = "FindTarget",
    OnUpdate = () =>
    {
        target = ScanForEnemy();
        if (target != null) findTarget.Succeed();
        else if (searchTimeout) findTarget.Fail();
    }
};

var attack = new CyclopsState { Name = "Attack" };
var patrol = new CyclopsState { Name = "Patrol" };

findTarget.OnSuccess(attack);
findTarget.OnFailure(patrol);
```

### Exit Transition

```csharp
state.AddExitTransition(nextState); // Triggers when state.Stop() is called
```

## Best Practice: Bootstrap-Only Access

**Fully configure the state machine at bootstrap, then never expose it again.**

The library is designed around this principle:
- States have no reference to their host machine — they can only signal transitions via pre-declared conditions
- States call `Stop()`, `Succeed()`, or `Fail()` to request exit, but don't control what happens next
- The machine's `Update()` loop evaluates all transitions — states don't imperatively push other states

**Why this matters:**
- **Predictability**: The entire state graph is known upfront and can be reasoned about
- **Testability**: You can verify reachable states from transitions alone
- **Decoupling**: States don't know about the machine or each other — only their own lifecycle

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

1. **Composition over inheritance**: Single sealed `CyclopsState` class — no subclassing required
2. **State reuse**: States can be re-entered after exiting (new `CancellationTokenSource` created)
3. **Transition pooling**: Transitions list uses `ListPool<T>` — call `Dispose()` when done
4. **Force stop**: `CyclopsStateMachine.ForceStop()` immediately stops all states in order
5. **No transition removal**: By design, transitions cannot be removed once added
6. **BT Result**: Defaults to `BtResult.Running`; set via `Succeed()` or `Fail()`

## Testing

Unit tests cover:
- Push/Pop/Replace transition mechanics
- Exit-on-action behavior
- Lifecycle callback counts (enter, update, background, exit)
- Background mode transitions
- Force stop ordering
- State reuse and cancellation token renewal

## Dependencies

- Unity 2023.3+ / Unity 6000
- Uses: `UnityEngine.Pool`, `UnityEngine.Awaitable`, `System.Threading.CancellationToken`

## Future Considerations

### Event Dispatch System (Under Consideration)

A DOM/Flash-style event propagation system for the state stack:

- **Capture phase**: Events dispatch from top of stack downward (foreground → background)
- States register type-safe handlers via `Handle<T>(Func<T, DispatchResult>)`
- Handlers return `Consume` (stop propagation) or `Propagate` (continue to next state)
- Zero-boxing design using static generic dictionaries

**Primary use case**: Input handling for layered UI (modals blocking input, pause menus, etc.)

```csharp
// Proposed API sketch
var pauseMenu = new CyclopsState { Name = "PauseMenu" }
    .HandleAndConsume<PauseInput>(_ => Resume())   // Blocks propagation
    .HandleAndConsume<MoveInput>(_ => { });        // Swallows movement

// On CyclopsStateMachine
fsm.Dispatch(new JumpInput());  // Top state gets first crack
```

**Design notes**:
- Keep separate from BT result flow (transitions handle that well already)
- Bridge class for Unity Input System integration (keeps core dependency-free)
- Consider whether bubble phase (bottom → top) is needed (probably not for linear stack)

**Status**: Deferred for later evaluation. Current transitions + lifecycle hooks may suffice for most cases.