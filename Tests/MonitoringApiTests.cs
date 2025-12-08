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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class MonitoringApiTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear caches before each test to ensure isolation
            CyclopsStateMachine.ClearMonitoringCaches();
        }
        
        [TearDown]
        public void TearDown()
        {
            CyclopsStateMachine.ClearMonitoringCaches();
        }

        #region StateCount Tests

        [Test]
        public void StateCount_IsZero_WhenEmpty()
        {
            var fsm = new CyclopsStateMachine();
            Assert.AreEqual(0, fsm.StateCount);
        }

        [Test]
        public void StateCount_IsOne_AfterPushAndUpdate()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            fsm.PushState(state);
            fsm.Update();
            
            Assert.AreEqual(1, fsm.StateCount);
        }

        [Test]
        public void StateCount_ReflectsStackDepth()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            fsm.PushState(stateA);
            fsm.Update();
            
            Assert.AreEqual(1, fsm.StateCount);
            
            fsm.Update(); // Triggers push transition
            fsm.Update(); // stateB enters stack
            
            Assert.AreEqual(2, fsm.StateCount);
        }

        #endregion

        #region WithStateStackSnapshot Action Tests

        [Test]
        public void WithStateStackSnapshot_InvokesAction()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            bool actionCalled = false;
            fsm.WithStateStackSnapshot(states =>
            {
                actionCalled = true;
            });
            
            Assert.IsTrue(actionCalled);
        }

        [Test]
        public void WithStateStackSnapshot_PassesCorrectCount()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            fsm.PushState(stateA);
            fsm.Update();
            fsm.Update();
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(2, states.Count);
            });
        }

        [Test]
        public void WithStateStackSnapshot_EmptyStack_PassesEmptyList()
        {
            var fsm = new CyclopsStateMachine();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(0, states.Count);
            });
        }

        [Test]
        public void WithStateStackSnapshot_StatesAreBottomToTop()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            fsm.PushState(stateA);
            fsm.Update();
            fsm.Update();
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                // Index 0 is bottom (stateA), index 1 is top (stateB)
                Assert.IsFalse(states[0].IsForegroundState);
                Assert.IsTrue(states[1].IsForegroundState);
            });
        }

        #endregion

        #region WithStateStackSnapshot Func Tests

        [Test]
        public void WithStateStackSnapshot_Func_ReturnsValue()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            int result = fsm.WithStateStackSnapshot(states => states.Count * 10);
            
            Assert.AreEqual(10, result);
        }

        [Test]
        public void WithStateStackSnapshot_Func_CanComputeFromStates()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            fsm.PushState(stateA);
            fsm.Update();
            fsm.Update();
            fsm.Update();
            
            int activeCount = fsm.WithStateStackSnapshot(states => 
                states.Count(s => s.IsActive));
            
            Assert.AreEqual(2, activeCount);
        }

        #endregion

        #region VisitStateStack Tests

        [Test]
        public void VisitStateStack_InvokesForEachState()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            fsm.PushState(stateA);
            fsm.Update();
            fsm.Update();
            fsm.Update();
            
            var visited = new List<int>();
            fsm.VisitStateStack((index, state) =>
            {
                visited.Add(index);
            });
            
            Assert.AreEqual(2, visited.Count);
            Assert.AreEqual(0, visited[0]);
            Assert.AreEqual(1, visited[1]);
        }

        [Test]
        public void VisitStateStack_EmptyStack_DoesNotInvoke()
        {
            var fsm = new CyclopsStateMachine();
            
            int callCount = 0;
            fsm.VisitStateStack((index, state) =>
            {
                callCount++;
            });
            
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void VisitStateStack_ProvidesCorrectSnapshots()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            fsm.PushState(stateA);
            fsm.Update();
            fsm.Update();
            fsm.Update();
            
            var snapshots = new List<StateSnapshot>();
            fsm.VisitStateStack((index, state) =>
            {
                snapshots.Add(state);
            });
            
            Assert.AreEqual("CyclopsState", snapshots[0].ShortName);
            Assert.AreEqual("CyclopsState", snapshots[1].ShortName);
            Assert.IsFalse(snapshots[0].IsForegroundState);
            Assert.IsTrue(snapshots[1].IsForegroundState);
        }

        #endregion

        #region StateSnapshot Properties Tests

        [Test]
        public void StateSnapshot_ShortName_IsClassName()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("CyclopsState", states[0].ShortName);
            });
        }

        [Test]
        public void StateSnapshot_Name_IsFullTypeName()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.IsTrue(states[0].Name.Contains("CyclopsState"));
                Assert.IsTrue(states[0].Name.Contains("Cyclops.States"));
            });
        }

        [Test]
        public void StateSnapshot_IsActive_ReflectsState()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.IsTrue(states[0].IsActive);
            });
        }

        [Test]
        public void StateSnapshot_IsForegroundState_ReflectsState()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            fsm.PushState(stateA);
            fsm.Update();
            fsm.Update();
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.IsFalse(states[0].IsForegroundState); // Bottom
                Assert.IsTrue(states[1].IsForegroundState);  // Top
            });
        }

        #endregion

        #region Transition Snapshot Tests

        [Test]
        public void StateSnapshot_Transitions_IsEmpty_WhenNoTransitions()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(0, states[0].Transitions.Count);
            });
        }

        [Test]
        public void StateSnapshot_Transitions_ReflectsAddedTransitions()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, () => false);
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(1, states[0].Transitions.Count);
            });
        }

        [Test]
        public void TransitionSnapshot_Op_ReflectsReplaceTransition()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, () => false);
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(StackOp.Replace, states[0].Transitions[0].Op);
            });
        }

        [Test]
        public void TransitionSnapshot_Op_ReflectsPushTransition()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => false);
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(StackOp.Push, states[0].Transitions[0].Op);
            });
        }

        [Test]
        public void TransitionSnapshot_Op_ReflectsPopTransition()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            state.AddPopTransition(() => false);
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(StackOp.Pop, states[0].Transitions[0].Op);
            });
        }

        [Test]
        public void TransitionSnapshot_TargetName_IsNull_ForPopTransition()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            state.AddPopTransition(() => false);
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.IsNull(states[0].Transitions[0].TargetName);
                Assert.IsNull(states[0].Transitions[0].TargetShortName);
            });
        }

        [Test]
        public void TransitionSnapshot_TargetName_ReflectsTargetState()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, () => false);
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("CyclopsState", states[0].Transitions[0].TargetShortName);
                Assert.IsTrue(states[0].Transitions[0].TargetName.Contains("CyclopsState"));
            });
        }

        [Test]
        public void StateSnapshot_Transitions_ReflectsMultipleTransitions()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            var stateC = new CyclopsState();
            
            stateA.AddTransition(stateB, () => false);
            stateA.AddPushTransition(stateC, () => false);
            stateA.AddPopTransition(() => false);
            
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(3, states[0].Transitions.Count);
                Assert.AreEqual(StackOp.Replace, states[0].Transitions[0].Op);
                Assert.AreEqual(StackOp.Push, states[0].Transitions[1].Op);
                Assert.AreEqual(StackOp.Pop, states[0].Transitions[2].Op);
            });
        }

        #endregion

        #region ClearMonitoringCaches Tests

        [Test]
        public void ClearMonitoringCaches_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CyclopsStateMachine.ClearMonitoringCaches());
        }

        [Test]
        public void ClearMonitoringCaches_CanBeCalledMultipleTimes()
        {
            CyclopsStateMachine.ClearMonitoringCaches();
            CyclopsStateMachine.ClearMonitoringCaches();
            CyclopsStateMachine.ClearMonitoringCaches();
            
            // No exception means success
            Assert.Pass();
        }

        [Test]
        public void ClearMonitoringCaches_AfterUse_DoesNotBreakFunctionality()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            // Use the monitoring API to populate caches
            fsm.WithStateStackSnapshot(states => { });
            
            // Clear caches
            CyclopsStateMachine.ClearMonitoringCaches();
            
            // Should still work
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(1, states.Count);
                Assert.AreEqual("CyclopsState", states[0].ShortName);
            });
        }

        #endregion

        #region Transition Cache Invalidation Tests

        [Test]
        public void TransitionCache_UpdatesWhenTransitionsAdded()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            fsm.PushState(stateA);
            fsm.Update();
            
            // First snapshot - no transitions
            int firstCount = 0;
            fsm.WithStateStackSnapshot(states =>
            {
                firstCount = states[0].Transitions.Count;
            });
            
            // Add a transition
            stateA.AddTransition(stateB, () => false);
            
            // Second snapshot - should see new transition
            int secondCount = 0;
            fsm.WithStateStackSnapshot(states =>
            {
                secondCount = states[0].Transitions.Count;
            });
            
            Assert.AreEqual(0, firstCount);
            Assert.AreEqual(1, secondCount);
        }

        #endregion
    }
}

