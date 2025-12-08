// Cyclops States
// 
// Copyright 2025 Mark Davis
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Cyclops.States.BehaviorTree
{
    /// <summary>
    /// Static factory for creating behavior tree nodes as <see cref="CyclopsState"/> instances.
    /// Composites manually tick their children rather than pushing them to the state stack,
    /// which is more faithful to BT semantics and ensures children are reusable across traversals.
    /// </summary>
    public static class Bt
    {
        // =========== CONSTANTS ===========

        /// <summary>
        /// Maximum iterations per tick to prevent infinite loops from synchronous children.
        /// If exceeded, the composite yields until the next frame.
        /// </summary>
        public const int MaxIterationsPerTick = 1000;

        // =========== HELPERS ===========

        /// <summary>
        /// Starts a child state for BT ticking. Sets IsForegroundState so OnUpdate fires.
        /// </summary>
        private static void StartChild(CyclopsState child)
        {
            child.IsForegroundState = true;
            child.Start();
        }

        // =========== COMPOSITES ===========

        /// <summary>
        /// Runs children in order. Succeeds if all succeed; fails on first failure.
        /// Children are ticked directly by the Sequence, not pushed to the state stack.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="children">Child states to run in order.</param>
        /// <returns>A state that orchestrates the sequence.</returns>
        public static CyclopsState Sequence(string name, params CyclopsState[] children)
        {
            int index = 0;
            CyclopsState sequence = null;

            sequence = new CyclopsState
            {
                Name = name,
                OnEnter = () =>
                {
                    index = 0;
                    if (children.Length == 0)
                    {
                        sequence.Succeed();
                        return;
                    }
                    StartChild(children[0]);
                },
                OnUpdate = () =>
                {
                    // Loop to handle synchronously-completing children in a single tick
                    for (int iterations = 0; iterations < MaxIterationsPerTick && index < children.Length; iterations++)
                    {
                        var child = children[index];
                        child.Update();

                        if (child.Result == BtResult.Success)
                        {
                            child.StopImmediately();
                            index++;
                            
                            if (index >= children.Length)
                            {
                                sequence.Succeed();
                                return;
                            }
                            
                            StartChild(children[index]);
                            // Continue loop to process next child if it completes synchronously
                        }
                        else if (child.Result == BtResult.Failure)
                        {
                            child.StopImmediately();
                            sequence.Fail();
                            return;
                        }
                        else
                        {
                            // Running = continue next frame
                            return;
                        }
                    }
                },
                OnExit = () =>
                {
                    // Ensure any running child is stopped
                    if (index < children.Length && children[index].IsActive)
                        children[index].StopImmediately();
                }
            };

            return sequence;
        }

        /// <summary>
        /// Runs children in order. Succeeds on first success; fails if all fail.
        /// Children are ticked directly by the Selector, not pushed to the state stack.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="children">Child states to try in order.</param>
        /// <returns>A state that orchestrates the selection.</returns>
        public static CyclopsState Selector(string name, params CyclopsState[] children)
        {
            int index = 0;
            CyclopsState selector = null;

            selector = new CyclopsState
            {
                Name = name,
                OnEnter = () =>
                {
                    index = 0;
                    if (children.Length == 0)
                    {
                        selector.Fail();
                        return;
                    }
                    StartChild(children[0]);
                },
                OnUpdate = () =>
                {
                    // Loop to handle synchronously-completing children in a single tick
                    for (int iterations = 0; iterations < MaxIterationsPerTick && index < children.Length; iterations++)
                    {
                        var child = children[index];
                        child.Update();

                        if (child.Result == BtResult.Success)
                        {
                            child.StopImmediately();
                            selector.Succeed();
                            return;
                        }
                        else if (child.Result == BtResult.Failure)
                        {
                            child.StopImmediately();
                            index++;
                            
                            if (index >= children.Length)
                            {
                                selector.Fail();
                                return;
                            }
                            
                            StartChild(children[index]);
                            // Continue loop to process next child if it completes synchronously
                        }
                        else
                        {
                            // Running = continue next frame
                            return;
                        }
                    }
                },
                OnExit = () =>
                {
                    // Ensure any running child is stopped
                    if (index < children.Length && children[index].IsActive)
                        children[index].StopImmediately();
                }
            };

            return selector;
        }

        // =========== DECORATORS ===========

        /// <summary>
        /// Inverts child result: Success → Failure, Failure → Success.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="child">Child state to invert.</param>
        /// <returns>A state that inverts the child's result.</returns>
        public static CyclopsState Inverter(string name, CyclopsState child)
        {
            CyclopsState inverter = null;

            inverter = new CyclopsState
            {
                Name = name,
                OnEnter = () => StartChild(child),
                OnUpdate = () =>
                {
                    child.Update();

                    if (child.Result == BtResult.Success)
                    {
                        child.StopImmediately();
                        inverter.Fail();
                    }
                    else if (child.Result == BtResult.Failure)
                    {
                        child.StopImmediately();
                        inverter.Succeed();
                    }
                },
                OnExit = () =>
                {
                    if (child.IsActive)
                        child.StopImmediately();
                }
            };

            return inverter;
        }

        /// <summary>
        /// Always succeeds, regardless of child result.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="child">Child state to wrap.</param>
        /// <returns>A state that always succeeds when child completes.</returns>
        public static CyclopsState Succeeder(string name, CyclopsState child)
        {
            CyclopsState succeeder = null;

            succeeder = new CyclopsState
            {
                Name = name,
                OnEnter = () => StartChild(child),
                OnUpdate = () =>
                {
                    child.Update();

                    if (child.Result != BtResult.Running)
                    {
                        child.StopImmediately();
                        succeeder.Succeed();
                    }
                },
                OnExit = () =>
                {
                    if (child.IsActive)
                        child.StopImmediately();
                }
            };

            return succeeder;
        }

        /// <summary>
        /// Always fails, regardless of child result.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="child">Child state to wrap.</param>
        /// <returns>A state that always fails when child completes.</returns>
        public static CyclopsState Failer(string name, CyclopsState child)
        {
            CyclopsState failer = null;

            failer = new CyclopsState
            {
                Name = name,
                OnEnter = () => StartChild(child),
                OnUpdate = () =>
                {
                    child.Update();

                    if (child.Result != BtResult.Running)
                    {
                        child.StopImmediately();
                        failer.Fail();
                    }
                },
                OnExit = () =>
                {
                    if (child.IsActive)
                        child.StopImmediately();
                }
            };

            return failer;
        }

        /// <summary>
        /// Repeats child N times (or forever if count is -1). Fails if child ever fails.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="count">Number of repetitions (-1 for infinite).</param>
        /// <param name="child">Child state to repeat.</param>
        /// <returns>A state that repeats the child.</returns>
        public static CyclopsState Repeat(string name, int count, CyclopsState child)
        {
            int remaining = 0;
            CyclopsState repeat = null;

            repeat = new CyclopsState
            {
                Name = name,
                OnEnter = () =>
                {
                    remaining = count;
                    
                    // Zero repetitions = immediate success
                    if (count == 0)
                    {
                        repeat.Succeed();
                        return;
                    }
                },
                OnUpdate = () =>
                {
                    // Already completed in OnEnter (e.g., count == 0)
                    if (repeat.Result != BtResult.Running)
                        return;
                    
                    // Loop to handle synchronously-completing children in a single tick
                    for (int iterations = 0; iterations < MaxIterationsPerTick; iterations++)
                    {
                        // Start child if not already running
                        if (!child.IsActive)
                            StartChild(child);
                        
                        child.Update();

                        if (child.Result == BtResult.Success)
                        {
                            child.StopImmediately();
                            
                            if (remaining > 0)
                                remaining--;
                            
                            if (remaining == 0 && count != -1)
                            {
                                repeat.Succeed();
                                return;
                            }
                            // Continue loop - child will be restarted at top of next iteration
                        }
                        else if (child.Result == BtResult.Failure)
                        {
                            child.StopImmediately();
                            repeat.Fail();
                            return;
                        }
                        else
                        {
                            // Running = continue next frame
                            return;
                        }
                    }
                },
                OnExit = () =>
                {
                    if (child.IsActive)
                        child.StopImmediately();
                }
            };

            return repeat;
        }

        /// <summary>
        /// Retries child on failure up to maxAttempts times. Succeeds on first success.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="maxAttempts">Maximum number of attempts.</param>
        /// <param name="child">Child state to retry.</param>
        /// <returns>A state that retries the child on failure.</returns>
        public static CyclopsState Retry(string name, int maxAttempts, CyclopsState child)
        {
            int remaining = 0;
            CyclopsState retry = null;

            retry = new CyclopsState
            {
                Name = name,
                OnEnter = () =>
                {
                    remaining = maxAttempts;
                },
                OnUpdate = () =>
                {
                    // Loop to handle synchronously-completing children in a single tick
                    for (int iterations = 0; iterations < MaxIterationsPerTick; iterations++)
                    {
                        // Start child if not already running
                        if (!child.IsActive)
                            StartChild(child);
                        
                        child.Update();

                        if (child.Result == BtResult.Success)
                        {
                            child.StopImmediately();
                            retry.Succeed();
                            return;
                        }
                        else if (child.Result == BtResult.Failure)
                        {
                            child.StopImmediately();
                            remaining--;
                            
                            if (remaining <= 0)
                            {
                                retry.Fail();
                                return;
                            }
                            // Continue loop - child will be restarted at top of next iteration
                        }
                        else
                        {
                            // Running = continue next frame
                            return;
                        }
                    }
                },
                OnExit = () =>
                {
                    if (child.IsActive)
                        child.StopImmediately();
                }
            };

            return retry;
        }

        // =========== LEAF NODES ===========

        /// <summary>
        /// One-shot action that succeeds immediately after executing.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="action">Action to execute.</param>
        /// <returns>A state that runs the action and succeeds.</returns>
        public static CyclopsState Action(string name, Action action)
        {
            CyclopsState node = null;

            node = new CyclopsState
            {
                Name = name,
                OnEnter = () =>
                {
                    action();
                    node.Succeed();
                }
            };

            return node;
        }

        /// <summary>
        /// Action that runs each frame until it returns a non-Running result.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="tick">Tick function returning Running, Success, or Failure.</param>
        /// <returns>A state that ticks until completion.</returns>
        public static CyclopsState Action(string name, Func<BtResult> tick)
        {
            CyclopsState node = null;

            node = new CyclopsState
            {
                Name = name,
                OnUpdate = () =>
                {
                    var result = tick();
                    if (result == BtResult.Success)
                        node.Succeed();
                    else if (result == BtResult.Failure)
                        node.Fail();
                }
            };

            return node;
        }

        /// <summary>
        /// Evaluates condition: succeeds if true, fails if false.
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="predicate">Condition to evaluate.</param>
        /// <returns>A state that checks the condition.</returns>
        public static CyclopsState Condition(string name, Func<bool> predicate)
        {
            CyclopsState node = null;

            node = new CyclopsState
            {
                Name = name,
                OnEnter = () =>
                {
                    if (predicate())
                        node.Succeed();
                    else
                        node.Fail();
                }
            };

            return node;
        }

        /// <summary>
        /// Waits for the specified duration, then succeeds.
        /// Uses frame counting for consistency (not wall-clock time).
        /// </summary>
        /// <param name="name">Debug name for the node.</param>
        /// <param name="frames">Number of frames to wait.</param>
        /// <returns>A state that waits then succeeds.</returns>
        public static CyclopsState WaitFrames(string name, int frames)
        {
            int remaining = 0;
            CyclopsState node = null;

            node = new CyclopsState
            {
                Name = name,
                OnEnter = () => remaining = frames,
                OnUpdate = () =>
                {
                    remaining--;
                    if (remaining <= 0)
                        node.Succeed();
                }
            };

            return node;
        }
    }
}

