# About Cyclops States

### Why?
* I wanted a simple lightweight stack-based state machine that could double as a classic FSM at each level.
* It needed to be easy to create in code with minimal fuss.
* It needed to be easy to read, meaning a contiguous linear flow resembling a table of contents.
* As much as possible, transitions needed to be external, a visible part of that "table of contents".
* It needed to improve working with async/await in Unity, so that async lifetimes could be tied to state lifetimes.
* It needed the concept of foreground and background states, allowing special background updates when needed.
* It had to be as determinstic as possible, allowing predictable setup and unwinding in the expected order while being failure resistant.
* It also had to be easy to extend inline without creating new classes, but you can if you'd prefer.

### What else?
* It can operate as an FSM, pushdown automaton, or even a behavior tree depending on perspective and usage. Creating a behavior tree requires writing custom nodes, but the shape is right.
* It features predicate driven transitions that go beyond typical state replacement. Transitions that push, pop, and react to Actions and Action events are included.
* Transition related callbacks are included for just about every situation that might be needed.

### States Are Stackable

In addition to transitioning from one state to another, states can also be stacked. When a new state (such as a modal dialog) is pushed onto the stack without being the result of a transition from another state, the state below will enter a background update mode. You can provide whatever functionality you feel is appropriate for a background update vs a foreground update.

There's a reasonable chance that you'll want to forego some input processing when a state is in the background, but provide that processing during a foreground update. Perhaps your game uses a development console that should be accessible from anywhere. You might want to include the keyboard toggle (maybe the [ ` ] key?) in the background update of the lowest state in the stack. That way, your console will always be accessible.

If the top level state exits without a transition to another state, it's popped off the stack, and the new topmost state will transition from background mode to foreground mode.

### Async Lifetimes: Fire And Forget

This is a real quality of life feature. Each state contains an ExitCancellationToken that empowers you with the ability to link the lifetime of an async to the lifetime of a logical game state no matter how large or small. By providing the async with a state's own myState.ExitCancelationToken, you know for sure, that when a state exits, it will cancel all the asyncs that are logically associated with it. So for example, when a Gameplay state transitions to a GameOver state, all asyncs associated with Gameplay will be canceled and cleaned up. No worries. It's fully fire and forget.

You may be wondering... what if I hit the stop button and my game immediately exits from some arbitrary state in the editor? Won't those asyncs keep running in the background even when we're not in play mode? It's pretty annoying when they do that.

Good news here. Regardless of how your app exits, as long as there's not a blocking bug within your state's exit handler, you're fine. State machines can be forceably stopped. Just ensure that your state machine forceably stops when the application stops and all states will be exited automatically behind the scenes and in the expected order.

### States Can Have Multiple Transitions

Each state can be provided with an unlimited number of transitions to other states. Transitions contain a target state to transition to and a predicate that indicates when that transition should be occur.

#### Exit Transitions
Exit transitions are used when a state is manually stopped, typically from within it's own logic. These transitions do not expose their predicate. Simply provide a target state, and you're good to go. As mentioned above, when an exit transition doesn't exist, instead of being replaced, the state will be popped off the stack and if another state exists below, it will become the active state updating in the foreground.

#### Scope, Best Practices, And Common Pitfalls
For all other transitions, it's generally best to reason about a state using information external to that state. If a transition relies on the specific existance or properties of a state then it may not be possible to simply swap in a different state like a composable building block. Dependency injection can make this easy.

## Origin
This project and the following docs were borrowed directly from [Cyclops Framework](https://github.com/darkmavis/com.smonch.cyclopsframework). This is a stripped down version of that framework that focuses on the state machine section which was added at years after the original development began. 

# Awaitable Integration

[CyclopsBaseState](./Runtime/States/CyclopsBaseState.cs) supports the following
[Awaitable](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/Awaitable.html)
methods with automatic cancellation token handling:
</br>```
public async Awaitable FixedUpdateAsync()```
</br>```
public async Awaitable FromAsyncOperation(AsyncOperation op)```
</br>```
public async Awaitable NextFrameAsync()```
</br>```
public async Awaitable EndOfFrameAsync()```
```csharp
public async Awaitable WaitForSecondsAsync(float seconds)
{
    try
    {
        await Awaitable.WaitForSecondsAsync(seconds, ExitCancellationToken);
    }
    catch (OperationCanceledException)
    {
        // ignored
    }
}
```

States were already compatible with async/await, but now have tight integration with Unity's Awaitable as well.
The aim is to make Awaitable easier to use and more robust than it currently is with tight state machine integration.
Awaitable by itself currently requires manually tracking cancellation tokens or handling exceptions because it doesn't have proper state information.
Cyclops state machines naturally provide that information and will now wrap and handle all state management for the Awaitable methods automatically.
Behind the scenes, when a state is entered, a new CancellationToken is created. When a state exits, the CancellationToken is canceled.

```csharp
var loader = new CyclopsState();
loader.Entered = async () =>
{
    Debug.Log("Loader: Entered");
    
    for (int i = 0; i < 10 && loader.IsActive; ++i)
    {
        await loader.WaitForSecondsAsync(1f);
        Debug.Log($"Loader: {i}");
    }
    
    loader.Stop();
};
loader.Exited = () => Debug.Log("Loader: Exited");
```

# Installing Cyclops States

Cyclops States can be added to a Unity project via Unity's [Package Manager](https://docs.unity3d.com/Manual/upm-ui.html).
There are no install scripts and no unusual steps are required.

## State Machines
[`CyclopsStateMachine`](./Runtime/States/CyclopsStateMachine.cs) operates as an [FSM](https://gameprogrammingpatterns.com/state.html)
that also supports layered states via a state stack [(push-down automata)](https://gameprogrammingpatterns.com/state.html) if desired. 

### CyclopsState
[`CyclopsState`](./Runtime/States/CyclopsState.cs) is designed for lightweight states. Please create as many state machines as needed.

## Example
```csharp
public class Bootstrap : MonoBehaviour
{
    [SerializeField]
    private Camera _gameplayCamera;
    
    private async void Awake()
    {
        var fsm = new CyclopsStateMachine();
        var gameplay = new Gameplay(_gameplayCamera);
        var unloader = new CyclopsState
        {
            Entered = () => Debug.Log("Unloader: Entered"),
            Exited = () => Debug.Log("Unloader: Exited")
        };
        
        gameplay.AddTransition(unloader, () => Keyboard.current.escapeKey.isPressed);
        fsm.PushState(gameplay);

        while (!Application.exitCancellationToken.IsCancellationRequested)
        {
            await Task.Yield();
            fsm.Update();
        }
    }
}
```
Other possibilities for adding the gameplay to unloader transition:
```csharp
gameplay.AddExitTransition(unloader);
gameplay.AddTransition(new CyclopsStateTransition { Target = unloader, Condition = () => Keyboard.current.escapeKey.isPressed });
```

# Technical details

## Requirements

This version of Cyclops States should be compatible with the following versions of Unity:

- 6000
- 2023.3

## Package contents

The following table indicates the folder structure of the Cyclops States package:

| Location    | Description                                                      |
| ----------- | ---------------------------------------------------------------- |
| `<Runtime>` | Root folder containing the source for Cyclops States.         |
| `<Tests>`   | Root folder containing the source for testing Cyclops States. |

## Document revision history

| Date         | Reason                                                                                                 |
|--------------| ------------------------------------------------------------------------------------------------------ |
| Jul 27, 2024 | Updated description and compatibility details.                                                         |

## License

[Apache License 2.0](LICENSE.md)
