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
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class PopTransitionTests
    {
        private Action _fooAction;
        private Action<int> _paramAction;
        
        [SetUp]
        public void SetUp()
        {
            _fooAction = null;
            _paramAction = null;
        }

        [Test]
        public void AddPopTransition_PopsState()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateB.AddPopTransition(() => true);
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.Update();
            stateMachine.Update();
            
            // After pop, stateA should be foreground again
            Assert.IsTrue(stateA.IsForegroundState);
            Assert.IsFalse(stateB.IsActive);
        }
        
        [Test]
        public void AddPopTransition_ViaAction_Pops()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateB.AddPopTransition(ref _fooAction);
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.Update();
            
            Assert.IsTrue(stateB.IsForegroundState);
            
            _fooAction?.Invoke();
            stateMachine.Update();
            
            // Foreground status is now immediate after pop
            Assert.IsFalse(stateB.IsActive);
            Assert.IsTrue(stateA.IsForegroundState);
        }
        
        [Test]
        public void AddPopTransition_ViaGenericAction_Pops()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateB.AddPopTransition(ref _paramAction);
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.Update();
            
            _paramAction?.Invoke(123);
            stateMachine.Update();
            
            // Foreground status is now immediate after pop
            Assert.IsFalse(stateB.IsActive);
            Assert.IsTrue(stateA.IsForegroundState);
        }
        
        [Test]
        public void AddPopTransition_OnLastState_MachineBecomesIdle()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            state.AddPopTransition(() => true);
            stateMachine.PushState(state);
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsTrue(stateMachine.IsIdle);
        }
        
        [Test]
        public void Pop_ImmediatelyPromotesNewTopToForeground()
        {
            // This test validates that IsForegroundState is accurate immediately
            // after a pop, not delayed until the next Update() cycle.
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            bool shouldPop = false;
            
            stateB.AddPopTransition(() => shouldPop);
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsForegroundState, "stateA should be background");
            Assert.IsTrue(stateB.IsForegroundState, "stateB should be foreground");
            
            shouldPop = true;
            stateMachine.Update(); // Single update pops stateB
            
            // Immediately after pop (same frame), stateA should be foreground
            Assert.IsTrue(stateA.IsForegroundState, "stateA should be foreground immediately after pop");
            Assert.IsFalse(stateB.IsActive, "stateB should be inactive");
            
            stateMachine.ForceStop();
        }
    }
}

