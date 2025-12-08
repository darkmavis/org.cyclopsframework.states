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
    public class ComplexScenarioTests
    {
        [Test]
        public void ModalOverlay_PushPop_Pattern()
        {
            // Simulates: HUD -> pause menu -> HUD (modal overlay pattern)
            var stateMachine = new CyclopsStateMachine();
            var hud = new CyclopsState();
            var pauseMenu = new CyclopsState();
            bool shouldPause = false;
            bool shouldResume = false;
            
            hud.AddPushTransition(pauseMenu, () => shouldPause);
            pauseMenu.AddPopTransition(() => shouldResume);
            
            // Start with HUD
            stateMachine.PushState(hud);
            stateMachine.Update();
            Assert.IsTrue(hud.IsForegroundState);
            
            // Trigger pause
            shouldPause = true;
            stateMachine.Update();
            shouldPause = false; // Reset flag so it doesn't re-trigger after pop
            stateMachine.Update();
            
            Assert.IsFalse(hud.IsForegroundState);
            Assert.IsTrue(pauseMenu.IsForegroundState);
            Assert.IsTrue(hud.IsActive); // HUD still active, just backgrounded
            
            // Resume
            shouldResume = true;
            stateMachine.Update();
            
            // Foreground status is now immediate after pop
            Assert.IsTrue(hud.IsForegroundState);
            Assert.IsFalse(pauseMenu.IsActive);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void StateMachine_Chain_ABCA()
        {
            // Tests: A -> B -> C -> A (circular FSM)
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            var stateC = new CyclopsState();
            
            int phase = 0;
            
            stateA.AddTransition(stateB, () => phase == 1);
            stateB.AddTransition(stateC, () => phase == 2);
            stateC.AddTransition(stateA, () => phase == 3);
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            Assert.IsTrue(stateA.IsActive);
            
            phase = 1;
            stateMachine.Update();
            stateMachine.Update();
            Assert.IsTrue(stateB.IsActive);
            Assert.IsFalse(stateA.IsActive);
            
            phase = 2;
            stateMachine.Update();
            stateMachine.Update();
            Assert.IsTrue(stateC.IsActive);
            Assert.IsFalse(stateB.IsActive);
            
            phase = 3;
            stateMachine.Update();
            stateMachine.Update();
            Assert.IsTrue(stateA.IsActive);
            Assert.IsFalse(stateC.IsActive);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void DeepStack_AllStatesReceiveBackgroundUpdates()
        {
            int[] updateCounts = new int[5];
            var stateMachine = new CyclopsStateMachine();
            var states = new CyclopsState[5];
            
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                states[i] = new CyclopsState
                {
                    OnUpdate = () => updateCounts[index]++,
                    OnBackgroundUpdate = () => updateCounts[index]++
                };
                stateMachine.PushState(states[i]);
            }
            
            stateMachine.Update();
            
            // All states should have received exactly one update
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(1, updateCounts[i], $"State {i} update count mismatch");
            }
            
            stateMachine.ForceStop();
        }
    }
}
