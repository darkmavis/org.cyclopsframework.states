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

using Cyclops.States.BehaviorTree;
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class BtTests
    {
        // =========== Sequence Tests ===========

        [Test]
        public void Sequence_Succeeds_WhenAllChildrenSucceed()
        {
            var fsm = new CyclopsStateMachine();
            int completedChildren = 0;
            
            var sequence = Bt.Sequence("Test",
                Bt.Action("A", () => completedChildren++),
                Bt.Action("B", () => completedChildren++),
                Bt.Action("C", () => completedChildren++)
            );
            
            fsm.PushState(sequence);
            fsm.Update();
            
            Assert.AreEqual(3, completedChildren);
            Assert.AreEqual(BtResult.Success, sequence.Result);
        }

        [Test]
        public void Sequence_Fails_OnFirstChildFailure()
        {
            var fsm = new CyclopsStateMachine();
            int completedChildren = 0;
            
            var sequence = Bt.Sequence("Test",
                Bt.Action("A", () => completedChildren++),
                Bt.Condition("Fail", () => false),
                Bt.Action("C", () => completedChildren++)
            );
            
            fsm.PushState(sequence);
            fsm.Update();
            
            Assert.AreEqual(1, completedChildren); // Only first child ran
            Assert.AreEqual(BtResult.Failure, sequence.Result);
        }

        [Test]
        public void Sequence_EmptyChildren_Succeeds()
        {
            var fsm = new CyclopsStateMachine();
            var sequence = Bt.Sequence("Empty");
            
            fsm.PushState(sequence);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Success, sequence.Result);
        }

        // =========== Selector Tests ===========

        [Test]
        public void Selector_Succeeds_OnFirstChildSuccess()
        {
            var fsm = new CyclopsStateMachine();
            int attemptedChildren = 0;
            
            var selector = Bt.Selector("Test",
                Bt.Condition("Fail1", () => { attemptedChildren++; return false; }),
                Bt.Condition("Success", () => { attemptedChildren++; return true; }),
                Bt.Condition("NotReached", () => { attemptedChildren++; return true; })
            );
            
            fsm.PushState(selector);
            fsm.Update();
            
            Assert.AreEqual(2, attemptedChildren); // Third child not reached
            Assert.AreEqual(BtResult.Success, selector.Result);
        }

        [Test]
        public void Selector_Fails_WhenAllChildrenFail()
        {
            var fsm = new CyclopsStateMachine();
            
            var selector = Bt.Selector("Test",
                Bt.Condition("Fail1", () => false),
                Bt.Condition("Fail2", () => false),
                Bt.Condition("Fail3", () => false)
            );
            
            fsm.PushState(selector);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Failure, selector.Result);
        }

        [Test]
        public void Selector_EmptyChildren_Fails()
        {
            var fsm = new CyclopsStateMachine();
            var selector = Bt.Selector("Empty");
            
            fsm.PushState(selector);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Failure, selector.Result);
        }

        // =========== Decorator Tests ===========

        [Test]
        public void Inverter_InvertsSuccess()
        {
            var fsm = new CyclopsStateMachine();
            var inverter = Bt.Inverter("Test", Bt.Condition("True", () => true));
            
            fsm.PushState(inverter);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Failure, inverter.Result);
        }

        [Test]
        public void Inverter_InvertsFailure()
        {
            var fsm = new CyclopsStateMachine();
            var inverter = Bt.Inverter("Test", Bt.Condition("False", () => false));
            
            fsm.PushState(inverter);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Success, inverter.Result);
        }

        [Test]
        public void Succeeder_AlwaysSucceeds()
        {
            var fsm = new CyclopsStateMachine();
            var succeeder = Bt.Succeeder("Test", Bt.Condition("False", () => false));
            
            fsm.PushState(succeeder);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Success, succeeder.Result);
        }

        [Test]
        public void Failer_AlwaysFails()
        {
            var fsm = new CyclopsStateMachine();
            var failer = Bt.Failer("Test", Bt.Condition("True", () => true));
            
            fsm.PushState(failer);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Failure, failer.Result);
        }

        [Test]
        public void Repeat_RunsChildMultipleTimes()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            var repeat = Bt.Repeat("Test", 3, Bt.Action("Count", () => runCount++));
            
            fsm.PushState(repeat);
            fsm.Update();
            
            Assert.AreEqual(3, runCount);
            Assert.AreEqual(BtResult.Success, repeat.Result);
        }

        [Test]
        public void Repeat_FailsIfChildFails()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            var repeat = Bt.Repeat("Test", 3, 
                Bt.Action("MaybeFail", () => 
                {
                    runCount++;
                    return runCount >= 2 ? BtResult.Failure : BtResult.Success;
                })
            );
            
            fsm.PushState(repeat);
            fsm.Update();
            
            Assert.AreEqual(2, runCount);
            Assert.AreEqual(BtResult.Failure, repeat.Result);
        }

        [Test]
        public void Retry_SucceedsOnEventualSuccess()
        {
            var fsm = new CyclopsStateMachine();
            int attempts = 0;
            
            var retry = Bt.Retry("Test", 3,
                Bt.Action("EventualSuccess", () =>
                {
                    attempts++;
                    return attempts >= 2 ? BtResult.Success : BtResult.Failure;
                })
            );
            
            fsm.PushState(retry);
            fsm.Update();
            
            Assert.AreEqual(2, attempts);
            Assert.AreEqual(BtResult.Success, retry.Result);
        }

        [Test]
        public void Retry_FailsAfterMaxAttempts()
        {
            var fsm = new CyclopsStateMachine();
            int attempts = 0;
            
            var retry = Bt.Retry("Test", 3, Bt.Condition("AlwaysFail", () => { attempts++; return false; }));
            
            fsm.PushState(retry);
            fsm.Update();
            
            Assert.AreEqual(3, attempts);
            Assert.AreEqual(BtResult.Failure, retry.Result);
        }

        // =========== Leaf Node Tests ===========

        [Test]
        public void Action_ExecutesAndSucceeds()
        {
            var fsm = new CyclopsStateMachine();
            bool executed = false;
            
            var action = Bt.Action("Test", () => executed = true);
            
            fsm.PushState(action);
            fsm.Update();
            
            Assert.IsTrue(executed);
            Assert.AreEqual(BtResult.Success, action.Result);
        }

        [Test]
        public void Condition_SucceedsWhenTrue()
        {
            var fsm = new CyclopsStateMachine();
            var condition = Bt.Condition("Test", () => true);
            
            fsm.PushState(condition);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Success, condition.Result);
        }

        [Test]
        public void Condition_FailsWhenFalse()
        {
            var fsm = new CyclopsStateMachine();
            var condition = Bt.Condition("Test", () => false);
            
            fsm.PushState(condition);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Failure, condition.Result);
        }

        [Test]
        public void WaitFrames_WaitsCorrectNumberOfFrames()
        {
            var fsm = new CyclopsStateMachine();
            var wait = Bt.WaitFrames("Test", 3);
            
            fsm.PushState(wait);
            
            fsm.Update(); // Frame 1
            Assert.AreEqual(BtResult.Running, wait.Result);
            
            fsm.Update(); // Frame 2
            Assert.AreEqual(BtResult.Running, wait.Result);
            
            fsm.Update(); // Frame 3
            Assert.AreEqual(BtResult.Success, wait.Result);
        }

        // =========== Reusability Tests ===========

        [Test]
        public void Sequence_CanBeRerun_AfterCompletion()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            var sequence = Bt.Sequence("Test",
                Bt.Action("Count", () => runCount++)
            );
            
            // First run
            fsm.PushState(sequence);
            fsm.Update();
            Assert.AreEqual(1, runCount);
            Assert.AreEqual(BtResult.Success, sequence.Result);
            
            // Simulate pop (sequence completed, removed from stack)
            fsm.ForceStop();
            
            // Second run - should reset and run again
            fsm.PushState(sequence);
            fsm.Update();
            Assert.AreEqual(2, runCount);
            Assert.AreEqual(BtResult.Success, sequence.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void NestedComposites_WorkCorrectly()
        {
            var fsm = new CyclopsStateMachine();
            int step = 0;
            
            var tree = Bt.Selector("Root",
                Bt.Sequence("TryPath1",
                    Bt.Condition("Check1", () => false),
                    Bt.Action("Path1", () => step = 1)
                ),
                Bt.Sequence("TryPath2",
                    Bt.Condition("Check2", () => true),
                    Bt.Action("Path2", () => step = 2)
                )
            );
            
            fsm.PushState(tree);
            fsm.Update();
            
            Assert.AreEqual(2, step); // Should have taken path 2
            Assert.AreEqual(BtResult.Success, tree.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void ChildOnExit_CalledWhenParentStops()
        {
            var fsm = new CyclopsStateMachine();
            bool childExited = false;
            
            var runningChild = new CyclopsState
            {
                Name = "RunningChild",
                OnExit = () => childExited = true
            };
            
            var sequence = Bt.Sequence("Test", runningChild);
            
            fsm.PushState(sequence);
            fsm.Update(); // Child starts running
            
            fsm.ForceStop(); // Force stop should trigger child's OnExit
            
            Assert.IsTrue(childExited);
        }

        // =========== Async/Multi-Frame Tests ===========

        [Test]
        public void Action_WithTickFunction_RunsOverMultipleFrames()
        {
            var fsm = new CyclopsStateMachine();
            int tickCount = 0;
            
            var action = Bt.Action("MultiFrame", () =>
            {
                tickCount++;
                return tickCount >= 3 ? BtResult.Success : BtResult.Running;
            });
            
            fsm.PushState(action);
            
            fsm.Update(); // Tick 1
            Assert.AreEqual(1, tickCount);
            Assert.AreEqual(BtResult.Running, action.Result);
            
            fsm.Update(); // Tick 2
            Assert.AreEqual(2, tickCount);
            Assert.AreEqual(BtResult.Running, action.Result);
            
            fsm.Update(); // Tick 3 - completes
            Assert.AreEqual(3, tickCount);
            Assert.AreEqual(BtResult.Success, action.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void Sequence_WithAsyncChild_WaitsForCompletion()
        {
            var fsm = new CyclopsStateMachine();
            int step = 0;
            int tickCount = 0;
            
            var sequence = Bt.Sequence("Test",
                Bt.Action("Step1", () => step = 1),
                Bt.Action("AsyncStep", () =>
                {
                    tickCount++;
                    return tickCount >= 2 ? BtResult.Success : BtResult.Running;
                }),
                Bt.Action("Step3", () => step = 3)
            );
            
            fsm.PushState(sequence);
            
            fsm.Update(); // Step1 completes, AsyncStep starts
            Assert.AreEqual(1, step);
            Assert.AreEqual(1, tickCount);
            Assert.AreEqual(BtResult.Running, sequence.Result);
            
            fsm.Update(); // AsyncStep completes, Step3 runs
            Assert.AreEqual(3, step);
            Assert.AreEqual(BtResult.Success, sequence.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void Selector_WithAsyncChild_WaitsForResult()
        {
            var fsm = new CyclopsStateMachine();
            int tickCount = 0;
            bool secondChildRan = false;
            
            var selector = Bt.Selector("Test",
                Bt.Action("AsyncFail", () =>
                {
                    tickCount++;
                    return tickCount >= 2 ? BtResult.Failure : BtResult.Running;
                }),
                Bt.Action("Fallback", () => { secondChildRan = true; })
            );
            
            fsm.PushState(selector);
            
            fsm.Update(); // AsyncFail running
            Assert.AreEqual(1, tickCount);
            Assert.IsFalse(secondChildRan);
            Assert.AreEqual(BtResult.Running, selector.Result);
            
            fsm.Update(); // AsyncFail fails, Fallback runs
            Assert.IsTrue(secondChildRan);
            Assert.AreEqual(BtResult.Success, selector.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void Inverter_WithAsyncChild_WaitsForResult()
        {
            var fsm = new CyclopsStateMachine();
            int tickCount = 0;
            
            var inverter = Bt.Inverter("Test",
                Bt.Action("AsyncSuccess", () =>
                {
                    tickCount++;
                    return tickCount >= 2 ? BtResult.Success : BtResult.Running;
                })
            );
            
            fsm.PushState(inverter);
            
            fsm.Update();
            Assert.AreEqual(BtResult.Running, inverter.Result);
            
            fsm.Update();
            Assert.AreEqual(BtResult.Failure, inverter.Result); // Inverted
            
            fsm.ForceStop();
        }

        [Test]
        public void Repeat_WithAsyncChild_WaitsEachIteration()
        {
            var fsm = new CyclopsStateMachine();
            int iterations = 0;
            int ticksPerIteration = 0;
            
            var repeat = Bt.Repeat("Test", 2,
                Bt.Action("AsyncWork", () =>
                {
                    ticksPerIteration++;
                    if (ticksPerIteration >= 2)
                    {
                        ticksPerIteration = 0;
                        iterations++;
                        return BtResult.Success;
                    }
                    return BtResult.Running;
                })
            );
            
            fsm.PushState(repeat);
            
            fsm.Update(); // Iteration 1, tick 1
            Assert.AreEqual(0, iterations);
            Assert.AreEqual(BtResult.Running, repeat.Result);
            
            // Iteration 1 tick 2 completes, then iteration 2 tick 1 starts immediately
            fsm.Update();
            Assert.AreEqual(1, iterations);
            Assert.AreEqual(BtResult.Running, repeat.Result);
            
            // Iteration 2 tick 2 - completes
            fsm.Update();
            Assert.AreEqual(2, iterations);
            Assert.AreEqual(BtResult.Success, repeat.Result);
            
            fsm.ForceStop();
        }

        // =========== Edge Case Tests ===========

        [Test]
        public void Repeat_ZeroCount_SucceedsImmediately()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            var repeat = Bt.Repeat("Zero", 0, Bt.Action("Never", () => runCount++));
            
            fsm.PushState(repeat);
            fsm.Update();
            
            Assert.AreEqual(0, runCount); // Child never runs
            Assert.AreEqual(BtResult.Success, repeat.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void WaitFrames_ZeroFrames_SucceedsImmediately()
        {
            var fsm = new CyclopsStateMachine();
            var wait = Bt.WaitFrames("Zero", 0);
            
            fsm.PushState(wait);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Success, wait.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void WaitFrames_OneFrame_SucceedsOnFirstUpdate()
        {
            var fsm = new CyclopsStateMachine();
            var wait = Bt.WaitFrames("One", 1);
            
            fsm.PushState(wait);
            fsm.Update();
            
            Assert.AreEqual(BtResult.Success, wait.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void Retry_OneAttempt_NoRetry()
        {
            var fsm = new CyclopsStateMachine();
            int attempts = 0;
            
            var retry = Bt.Retry("Once", 1, Bt.Condition("Fail", () => { attempts++; return false; }));
            
            fsm.PushState(retry);
            fsm.Update();
            
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(BtResult.Failure, retry.Result);
            
            fsm.ForceStop();
        }

        // =========== Decorator Reusability Tests ===========

        [Test]
        public void Inverter_CanBeRerun_AfterCompletion()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            var inverter = Bt.Inverter("Test", Bt.Action("Count", () => runCount++));
            
            // First run
            fsm.PushState(inverter);
            fsm.Update();
            Assert.AreEqual(1, runCount);
            Assert.AreEqual(BtResult.Failure, inverter.Result);
            
            fsm.ForceStop();
            
            // Second run
            fsm.PushState(inverter);
            fsm.Update();
            Assert.AreEqual(2, runCount);
            Assert.AreEqual(BtResult.Failure, inverter.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void Repeat_CanBeRerun_AfterCompletion()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            var repeat = Bt.Repeat("Test", 2, Bt.Action("Count", () => runCount++));
            
            // First run
            fsm.PushState(repeat);
            fsm.Update();
            Assert.AreEqual(2, runCount);
            Assert.AreEqual(BtResult.Success, repeat.Result);
            
            fsm.ForceStop();
            
            // Second run - should reset and run again
            fsm.PushState(repeat);
            fsm.Update();
            Assert.AreEqual(4, runCount);
            Assert.AreEqual(BtResult.Success, repeat.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void Selector_CanBeRerun_AfterCompletion()
        {
            var fsm = new CyclopsStateMachine();
            int failCount = 0;
            int successCount = 0;
            
            var selector = Bt.Selector("Test",
                Bt.Condition("Fail", () => { failCount++; return false; }),
                Bt.Action("Success", () => successCount++)
            );
            
            // First run
            fsm.PushState(selector);
            fsm.Update();
            Assert.AreEqual(1, failCount);
            Assert.AreEqual(1, successCount);
            
            fsm.ForceStop();
            
            // Second run
            fsm.PushState(selector);
            fsm.Update();
            Assert.AreEqual(2, failCount);
            Assert.AreEqual(2, successCount);
            
            fsm.ForceStop();
        }

        // =========== Child Lifecycle Tests ===========

        [Test]
        public void Sequence_Child_ReceivesFullLifecycle()
        {
            var fsm = new CyclopsStateMachine();
            int enterCount = 0;
            int updateCount = 0;
            int exitCount = 0;
            
            CyclopsState child = null;
            child = new CyclopsState
            {
                Name = "LifecycleChild",
                OnEnter = () => enterCount++,
                OnUpdate = () =>
                {
                    updateCount++;
                    if (updateCount >= 2)
                        child.Succeed();
                },
                OnExit = () => exitCount++
            };
            
            var sequence = Bt.Sequence("Test", child);
            
            fsm.PushState(sequence);
            
            fsm.Update(); // Child enters, updates once
            Assert.AreEqual(1, enterCount);
            Assert.AreEqual(1, updateCount);
            Assert.AreEqual(0, exitCount);
            
            fsm.Update(); // Child updates again and succeeds
            Assert.AreEqual(1, enterCount);
            Assert.AreEqual(2, updateCount);
            Assert.AreEqual(1, exitCount); // StopImmediately calls OnExit
            
            fsm.ForceStop();
        }

        [Test]
        public void ForceStop_StopsNestedChildren()
        {
            var fsm = new CyclopsStateMachine();
            bool innerExited = false;
            
            var innerChild = new CyclopsState
            {
                Name = "Inner",
                OnExit = () => innerExited = true
            };
            
            var outer = Bt.Sequence("Outer",
                Bt.Sequence("Middle", innerChild)
            );
            
            fsm.PushState(outer);
            fsm.Update(); // Start running
            
            fsm.ForceStop();
            
            Assert.IsTrue(innerExited);
        }

        [Test]
        public void Child_OnEnterBackground_NotCalled_ForBtChildren()
        {
            // BT children are manually ticked, not pushed to state stack,
            // so they shouldn't receive background callbacks
            var fsm = new CyclopsStateMachine();
            bool backgroundEntered = false;
            
            var child = new CyclopsState
            {
                Name = "Child",
                OnEnterBackground = () => backgroundEntered = true
            };
            
            var sequence = Bt.Sequence("Test", 
                child,
                Bt.WaitFrames("Wait", 5)
            );
            
            fsm.PushState(sequence);
            fsm.Update(); // Child runs and completes, WaitFrames starts
            
            // Child completed and was stopped, never went to "background"
            Assert.IsFalse(backgroundEntered);
            
            fsm.ForceStop();
        }

        // =========== Composite Iteration Limit Tests ===========

        [Test]
        public void Sequence_ManySyncChildren_HitsIterationLimit()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            // Create more children than MaxIterationsPerTick
            var children = new CyclopsState[Bt.MaxIterationsPerTick + 100];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = Bt.Action($"Action{i}", () => runCount++);
            }
            
            var sequence = Bt.Sequence("Big", children);
            
            fsm.PushState(sequence);
            fsm.Update(); // Should not hang
            
            // First child runs in OnEnter, then MaxIterationsPerTick more in the loop
            // Actually, looking at implementation: first child started in OnEnter,
            // then loop processes up to MaxIterationsPerTick transitions
            Assert.LessOrEqual(runCount, Bt.MaxIterationsPerTick + 1);
            Assert.AreEqual(BtResult.Running, sequence.Result);
            
            // Continue on next frame
            fsm.Update();
            Assert.AreEqual(children.Length, runCount);
            Assert.AreEqual(BtResult.Success, sequence.Result);
            
            fsm.ForceStop();
        }

        [Test]
        public void Selector_ManySyncFailures_HitsIterationLimit()
        {
            var fsm = new CyclopsStateMachine();
            int failCount = 0;
            
            // Create more children than MaxIterationsPerTick, all failing
            var children = new CyclopsState[Bt.MaxIterationsPerTick + 100];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = Bt.Condition($"Fail{i}", () => { failCount++; return false; });
            }
            
            var selector = Bt.Selector("Big", children);
            
            fsm.PushState(selector);
            fsm.Update();
            
            Assert.LessOrEqual(failCount, Bt.MaxIterationsPerTick + 1);
            Assert.AreEqual(BtResult.Running, selector.Result);
            
            fsm.Update();
            Assert.AreEqual(children.Length, failCount);
            Assert.AreEqual(BtResult.Failure, selector.Result);
            
            fsm.ForceStop();
        }

        // =========== Deeply Nested Tests ===========

        [Test]
        public void DeeplyNestedTree_WorksCorrectly()
        {
            var fsm = new CyclopsStateMachine();
            int depth = 0;
            int maxDepth = 0;
            
            // Build a deeply nested structure
            var tree = Bt.Sequence("L1",
                Bt.Action("Enter1", () => { depth++; maxDepth = System.Math.Max(maxDepth, depth); }),
                Bt.Selector("L2",
                    Bt.Condition("Fail", () => false),
                    Bt.Sequence("L3",
                        Bt.Action("Enter2", () => { depth++; maxDepth = System.Math.Max(maxDepth, depth); }),
                        Bt.Inverter("L4",
                            Bt.Sequence("L5",
                                Bt.Action("Enter3", () => { depth++; maxDepth = System.Math.Max(maxDepth, depth); }),
                                Bt.Condition("Fail", () => false)
                            )
                        ),
                        Bt.Action("Exit2", () => depth--)
                    )
                ),
                Bt.Action("Exit1", () => depth--)
            );
            
            fsm.PushState(tree);
            fsm.Update();
            
            Assert.AreEqual(3, maxDepth); // Reached 3 levels of Enter actions
            Assert.AreEqual(BtResult.Success, tree.Result);
            
            fsm.ForceStop();
        }

        // =========== Infinite Loop Protection Tests ===========

        [Test]
        public void Repeat_InfiniteWithSyncChild_DoesNotHang()
        {
            var fsm = new CyclopsStateMachine();
            int runCount = 0;
            
            // Infinite repeat with synchronous child - would hang without protection
            var repeat = Bt.Repeat("Infinite", -1, Bt.Action("Count", () => runCount++));
            
            fsm.PushState(repeat);
            fsm.Update(); // Should not hang
            
            // Should have hit the iteration limit
            Assert.AreEqual(Bt.MaxIterationsPerTick, runCount);
            Assert.AreEqual(BtResult.Running, repeat.Result); // Still running, not stuck
            
            // Can continue on next frame
            fsm.Update();
            Assert.AreEqual(Bt.MaxIterationsPerTick * 2, runCount);
            
            fsm.ForceStop();
        }

        [Test]
        public void Retry_InfiniteFailures_DoesNotHang()
        {
            var fsm = new CyclopsStateMachine();
            int attempts = 0;
            
            // Retry with always-failing sync child and high attempt count
            var retry = Bt.Retry("ManyRetries", Bt.MaxIterationsPerTick + 500, 
                Bt.Condition("AlwaysFail", () => { attempts++; return false; }));
            
            fsm.PushState(retry);
            fsm.Update(); // Should not hang
            
            // Should have hit the iteration limit, not the attempt limit
            Assert.AreEqual(Bt.MaxIterationsPerTick, attempts);
            Assert.AreEqual(BtResult.Running, retry.Result);
            
            // Continue on next frame - should complete (500 remaining attempts)
            fsm.Update();
            Assert.AreEqual(Bt.MaxIterationsPerTick + 500, attempts);
            Assert.AreEqual(BtResult.Failure, retry.Result); // Now exhausted
            
            fsm.ForceStop();
        }
    }
}

