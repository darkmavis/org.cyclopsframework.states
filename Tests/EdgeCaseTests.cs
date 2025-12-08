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
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class EdgeCaseTests
    {
        [Test]
        public void MultipleStates_PushedRapidly_AllStart()
        {
            var startOrder = new List<string>();
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState { Entered = () => startOrder.Add("A") };
            var stateB = new CyclopsState { Entered = () => startOrder.Add("B") };
            var stateC = new CyclopsState { Entered = () => startOrder.Add("C") };
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.PushState(stateC);
            stateMachine.Update();
            
            Assert.AreEqual(3, startOrder.Count);
            Assert.IsTrue(stateA.IsActive);
            Assert.IsTrue(stateB.IsActive);
            Assert.IsTrue(stateC.IsActive);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void Stop_CausesStateToExit_OnNextUpdate()
        {
            bool exitCalled = false;
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState { Exited = () => exitCalled = true };
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.IsTrue(state.IsActive);
            Assert.IsFalse(exitCalled);
            
            state.Stop();
            
            // State is still technically active until update processes the stop
            Assert.IsTrue(state.IsActive);
            
            stateMachine.Update();
            
            // Now the state should have exited
            Assert.IsFalse(state.IsActive);
            Assert.IsTrue(exitCalled);
        }
        
        [Test]
        public void TransitionCondition_EvaluatedEachFrame()
        {
            // Transitions are checked twice per Update() - before and after OnUpdate()
            int conditionChecks = 0;
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, () =>
            {
                ++conditionChecks;
                return false;
            });
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();
            
            // 3 updates * 2 checks per update = 6 checks
            Assert.AreEqual(6, conditionChecks);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void State_Stop_CanBeCalledMultipleTimes()
        {
            int exitCount = 0;
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState { Exited = () => ++exitCount };
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            state.Stop();
            state.Stop(); // Double stop
            state.Stop(); // Triple stop
            stateMachine.Update();
            
            Assert.AreEqual(1, exitCount);
        }
    }
}

