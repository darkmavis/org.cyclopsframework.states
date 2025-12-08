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
    public class StateMachineBasicTests
    {
        [Test]
        public void StateMachine_IsIdle_WhenEmpty()
        {
            var stateMachine = new CyclopsStateMachine();
            Assert.IsTrue(stateMachine.IsIdle);
        }
        
        [Test]
        public void StateMachine_IsNotIdle_AfterPushAndUpdate()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.IsFalse(stateMachine.IsIdle);
        }
        
        [Test]
        public void StateMachine_Update_OnEmptyStack_DoesNotThrow()
        {
            var stateMachine = new CyclopsStateMachine();
            Assert.DoesNotThrow(() => stateMachine.Update());
        }
        
        [Test]
        public void StateMachine_Context_ReflectsCurrentState()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.AreSame(state, stateMachine.Context);
        }
    }
}

