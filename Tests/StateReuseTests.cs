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
    public class StateReuseTests
    {
        [Test]
        public void State_CanBeReentered_AfterExit()
        {
            int enterCount = 0;
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState { OnEnter = () => ++enterCount };
            
            stateMachine.PushState(state);
            stateMachine.Update();
            Assert.AreEqual(1, enterCount);
            
            state.Stop();
            stateMachine.Update();
            Assert.IsFalse(state.IsActive);
            
            stateMachine.PushState(state);
            stateMachine.Update();
            Assert.AreEqual(2, enterCount);
            Assert.IsTrue(state.IsActive);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void State_CancellationToken_RenewedOnReentry()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            var token1 = state.ExitCancellationToken;
            Assert.IsFalse(token1.IsCancellationRequested);
            
            state.Stop();
            stateMachine.Update();
            Assert.IsTrue(token1.IsCancellationRequested);
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            var token2 = state.ExitCancellationToken;
            Assert.IsFalse(token2.IsCancellationRequested);
            Assert.AreNotSame(token1, token2);
            
            stateMachine.ForceStop();
        }
    }
}
