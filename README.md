# Cyclops States

A lightweight Unity state machine library that unifies **FSM**, **push-down automata**, and **behavior trees** into a single elegant model.

## Why Cyclops States?

- **One class, three paradigms** — `CyclopsState` works as FSM states, PDA stack frames, or BT nodes
- **Composition over inheritance** — No subclassing required; configure states with delegates
- **Visible control flow** — Transitions are declared upfront, making state graphs easy to read and reason about
- **Async-friendly** — Built-in Unity Awaitable integration with automatic cancellation
- **Deterministic cleanup** — States unwind in order with guaranteed `OnExit` calls

## Installation

Add to your Unity project via the [Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) using the git URL.

## Quick Start

### Basic FSM

```csharp
var fsm = new CyclopsStateMachine();

var gameplay = new CyclopsState
{
    Name = "Gameplay",
    OnEnter = () => Debug.Log("Game started!"),
    OnUpdate = () => { /* game logic */ }
};

var gameOver = new CyclopsState
{
    Name = "GameOver",
    OnEnter = () => Debug.Log("Game Over")
};

// Declare transition: gameplay → gameOver when player dies
gameplay.AddTransition(gameOver, () => player.IsDead);

fsm.PushState(gameplay);

// In your update loop:
void Update() => fsm.Update();
```

### Modal Overlay (Push/Pop)

```csharp
var hud = new CyclopsState { Name = "HUD" };
var pauseMenu = new CyclopsState { Name = "PauseMenu" };

// Escape pushes pause menu on top of HUD
hud.AddPushTransition(pauseMenu, () => Keyboard.current.escapeKey.wasPressedThisFrame);

// Escape pops pause menu, returning to HUD
pauseMenu.AddPopTransition(() => Keyboard.current.escapeKey.wasPressedThisFrame);

fsm.PushState(hud);
```

### Behavior Tree

```csharp
using Cyclops.States.BehaviorTree;

var ai = Bt.Selector("AI",
    Bt.Sequence("Attack",
        Bt.Condition("HasTarget", () => target != null),
        Bt.Action("Fire", () => weapon.Fire())
    ),
    Bt.Sequence("Search",
        Bt.Action("Scan", () =>
        {
            target = ScanForEnemy();
            return target != null ? BtResult.Success : BtResult.Failure;
        })
    ),
    Bt.Action("Patrol", () => patrol.Step())
);

fsm.PushState(ai);
```

## Core Concepts

### State Lifecycle

```
Start() → OnEnter → [OnUpdate | OnBackgroundUpdate] → OnExit
                              ↑
               OnEnterBackground / OnExitBackground
```

| Hook | When It's Called |
|------|------------------|
| `OnEnter` | State becomes active |
| `OnUpdate` | Each frame while foreground (top of stack) |
| `OnBackgroundUpdate` | Each frame while background (below top) |
| `OnEnterBackground` | Another state is pushed on top |
| `OnExitBackground` | State returns to foreground |
| `OnExit` | State is stopped or replaced |

### Transition Types

| Operation | What It Does |
|-----------|--------------|
| `AddTransition` | Replace current state with target |
| `AddPushTransition` | Push target on top, keep current |
| `AddPopTransition` | Pop current, resume state below |
| `AddExitTransition` | Trigger when `Stop()` is called |

```csharp
// Predicate-based (checked each frame)
state.AddTransition(target, () => someCondition);

// Action-based (fires when action is invoked)
state.AddTransition(target, ref onPlayerDied);
```

### Behavior Tree Nodes

The `Bt` factory creates states that work as BT nodes:

**Composites:**
```csharp
Bt.Sequence(name, children...)   // Succeeds if ALL children succeed
Bt.Selector(name, children...)   // Succeeds if ANY child succeeds
```

**Decorators:**
```csharp
Bt.Inverter(name, child)         // Success ↔ Failure
Bt.Succeeder(name, child)        // Always succeeds
Bt.Failer(name, child)           // Always fails
Bt.Repeat(name, count, child)    // Repeat N times (-1 = forever)
Bt.Retry(name, attempts, child)  // Retry on failure
```

**Leaves:**
```csharp
Bt.Action(name, () => { })                 // One-shot, succeeds immediately
Bt.Action(name, () => BtResult.Running)    // Tick until non-Running
Bt.Condition(name, () => true)             // Success if true, else failure
Bt.WaitFrames(name, frames)                // Wait N frames, then succeed
```

## Async Integration

States provide async helpers tied to their lifetime:

```csharp
var loading = new CyclopsState
{
    Name = "Loading",
    OnEnter = async () =>
    {
        await loading.WaitForSecondsAsync(2f);   // Auto-cancelled on exit
        await loading.NextFrameAsync();
        await loading.FromAsyncOperation(SceneManager.LoadSceneAsync("Game"));
        loading.Stop();
    }
};
```

Each state has an `ExitCancellationToken` that cancels when the state exits — no manual cleanup needed.

## Best Practice: Bootstrap-Only Access

Configure everything upfront, then just tick:

```csharp
// Bootstrap: wire the state graph once, then drive with async
var fsm = new CyclopsStateMachine();
var stateA = new CyclopsState();
var stateB = new CyclopsState();
stateA.AddTransition(stateB, () => someCondition);
fsm.PushState(stateA);

// Drive the loop — no MonoBehaviour needed
while (!fsm.IsIdle && !Application.exitCancellationToken.IsCancellationRequested)
{
    await Awaitable.NextFrameAsync(Application.exitCancellationToken);
    fsm.Update();
}
```

States don't hold references to the machine — they signal via `Stop()`, `Succeed()`, or `Fail()`, and transitions handle the rest. This makes state graphs predictable and testable.

## API Reference

### CyclopsStateMachine

| Method | Description |
|--------|-------------|
| `PushState(state)` | Add state to top of stack |
| `Update()` | Tick all states, process transitions |
| `ForceStop()` | Immediately stop all states in order |
| `IsIdle` | True when stack is empty |
| `StateCount` | Number of active states |

### CyclopsState

| Property | Description |
|----------|-------------|
| `Name` | Debug name for the state |
| `IsActive` | True while state is running |
| `IsForegroundState` | True if top of stack |
| `Result` | `BtResult.Running`, `Success`, or `Failure` |
| `ExitCancellationToken` | Cancelled when state exits |

| Method | Description |
|--------|-------------|
| `Stop()` | Request graceful exit |
| `Succeed()` | Set result to Success |
| `Fail()` | Set result to Failure |
| `AddTransition(target, condition)` | Replace on condition |
| `AddPushTransition(target, condition)` | Push on condition |
| `AddPopTransition(condition)` | Pop on condition |
| `OnSuccess(target)` | Transition on Success result |
| `OnFailure(target)` | Transition on Failure result |

## Requirements

- Unity 2023.3+ or Unity 6000+
- Uses: `UnityEngine.Pool`, `UnityEngine.Awaitable`

## Package Structure

| Folder | Contents |
|--------|----------|
| `Runtime/States` | Core state machine classes |
| `Runtime/BehaviorTree` | BT node factory |
| `Tests` | Unit tests |

## License

[Apache License 2.0](LICENSE.md)
