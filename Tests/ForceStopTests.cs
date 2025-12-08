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
    public class ForceStopTests
    {
        [Test]
        public void ForceStop_StopsAllStates_InOrder()
        {
            var exitOrder = new List<string>();
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState { Exited = () => exitOrder.Add("A") };
            var stateB = new CyclopsState { Exited = () => exitOrder.Add("B") };
            var stateC = new CyclopsState { Exited = () => exitOrder.Add("C") };
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.PushState(stateC);
            stateMachine.Update();
            
            stateMachine.ForceStop();
            
            // States should exit top-to-bottom (C, B, A)
            Assert.AreEqual(3, exitOrder.Count);
            Assert.AreEqual("C", exitOrder[0]);
            Assert.AreEqual("B", exitOrder[1]);
            Assert.AreEqual("A", exitOrder[2]);
        }
        
        [Test]
        public void ForceStop_MachineBecomesIdle()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.IsFalse(stateMachine.IsIdle);
            
            stateMachine.ForceStop();
            
            Assert.IsTrue(stateMachine.IsIdle);
        }
        
        [Test]
        public void ForceStop_CancelsPendingPushes()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            // Don't call Update() yet - states are queued
            
            stateMachine.ForceStop();
            
            Assert.IsTrue(stateMachine.IsIdle);
            Assert.IsFalse(stateA.IsActive);
            Assert.IsFalse(stateB.IsActive);
        }
    }
}

