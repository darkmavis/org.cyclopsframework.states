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
    public class PushTransitionTests
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
        public void AddPushTransition_IsPushed()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, () => true);
            stateMachine.PushState(stateA);
            
            Assert.IsTrue(stateA.IsForegroundState);
            
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsForegroundState);
            Assert.IsTrue(stateB.IsForegroundState);
        }
        
        [Test]
        public void AddPushTransition_ViaAction_Pushes()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, ref _fooAction);
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            Assert.IsTrue(stateA.IsForegroundState);
            
            _fooAction?.Invoke();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsForegroundState);
            Assert.IsTrue(stateB.IsForegroundState);
        }
        
        [Test]
        public void AddPushTransition_ViaGenericAction_Pushes()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddPushTransition(stateB, ref _paramAction);
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            Assert.IsTrue(stateA.IsForegroundState);
            
            _paramAction?.Invoke(42);
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsForegroundState);
            Assert.IsTrue(stateB.IsForegroundState);
        }
    }
}

