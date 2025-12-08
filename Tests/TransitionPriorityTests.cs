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

using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class TransitionPriorityTests
    {
        [Test]
        public void Transitions_FirstMatch_Wins()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            var stateC = new CyclopsState();
            
            // Both transitions are true, but first one should win
            stateA.AddTransition(stateB, () => true);
            stateA.AddTransition(stateC, () => true);
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsTrue(stateB.IsActive);
            Assert.IsFalse(stateC.IsActive);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void Transitions_NoMatch_StaysInState()
        {
            int updateCount = 0;
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState { Updating = () => ++updateCount };
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, () => false);
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsTrue(stateA.IsActive);
            Assert.IsFalse(stateB.IsActive);
            Assert.AreEqual(3, updateCount);
            
            stateMachine.ForceStop();
        }
    }
}

