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
    public class LifecycleCallbackTests
    {
        [Test]
        public void Update_CyclopsState_CountsAreCorrect()
        {
            int enteredCount = 0;
            int updatingCount = 0;
            int backgroundUpdatingCount = 0;
            int exitedCount = 0;
            int backgroundModeEnteredCount = 0;
            int backgroundModeExitedCount = 0;
            
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            stateA.Entered = () => ++enteredCount;
            stateA.Updating = () => ++updatingCount;
            stateA.Exited = () => ++exitedCount;
            stateA.BackgroundModeEntered = () => ++backgroundModeEnteredCount;
            stateA.BackgroundUpdating = () => ++backgroundUpdatingCount;
            stateA.BackgroundModeExited = () => ++backgroundModeExitedCount;
            
            stateMachine.PushState(stateA);
            
            Assert.AreEqual(0, enteredCount);
            Assert.AreEqual(0, updatingCount);
            Assert.AreEqual(0, exitedCount);
            Assert.AreEqual(0, backgroundModeEnteredCount);
            Assert.AreEqual(0, backgroundUpdatingCount);
            Assert.AreEqual(0, backgroundModeExitedCount);
            
            stateMachine.Update();
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(1, updatingCount);
            Assert.AreEqual(0, exitedCount);
            Assert.AreEqual(0, backgroundModeEnteredCount);
            Assert.AreEqual(0, backgroundUpdatingCount);
            Assert.AreEqual(0, backgroundModeExitedCount);
            
            stateMachine.Update();
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(2, updatingCount);
            Assert.AreEqual(0, exitedCount);
            Assert.AreEqual(0, backgroundModeEnteredCount);
            Assert.AreEqual(0, backgroundUpdatingCount);
            Assert.AreEqual(0, backgroundModeExitedCount);
            
            stateMachine.Update();
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(3, updatingCount);
            Assert.AreEqual(0, exitedCount);
            Assert.AreEqual(0, backgroundModeEnteredCount);
            Assert.AreEqual(0, backgroundUpdatingCount);
            Assert.AreEqual(0, backgroundModeExitedCount);
            
            var stateB = new CyclopsState();
            stateB.AddPopTransition(() => true);
            stateMachine.PushState(stateB);
            
            stateMachine.Update();
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(3, updatingCount);
            Assert.AreEqual(0, exitedCount);
            Assert.AreEqual(1, backgroundModeEnteredCount);
            Assert.AreEqual(1, backgroundUpdatingCount);
            Assert.AreEqual(0, backgroundModeExitedCount);
            
            stateMachine.Update();
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(4, updatingCount);
            Assert.AreEqual(0, exitedCount);
            Assert.AreEqual(1, backgroundModeEnteredCount);
            Assert.AreEqual(1, backgroundUpdatingCount);
            Assert.AreEqual(1, backgroundModeExitedCount);
            
            stateMachine.ForceStop();
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(4, updatingCount);
            Assert.AreEqual(1, exitedCount);
            Assert.AreEqual(1, backgroundModeEnteredCount);
            Assert.AreEqual(1, backgroundUpdatingCount);
            Assert.AreEqual(1, backgroundModeExitedCount);
            
            stateMachine.Update();
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(4, updatingCount);
            Assert.AreEqual(1, exitedCount);
            Assert.AreEqual(1, backgroundModeEnteredCount);
            Assert.AreEqual(1, backgroundUpdatingCount);
            Assert.AreEqual(1, backgroundModeExitedCount);
            
            stateMachine.PushState(stateA);
            
            Assert.AreEqual(1, enteredCount);
            Assert.AreEqual(4, updatingCount);
            Assert.AreEqual(1, exitedCount);
            Assert.AreEqual(1, backgroundModeEnteredCount);
            Assert.AreEqual(1, backgroundUpdatingCount);
            Assert.AreEqual(1, backgroundModeExitedCount);
            
            stateMachine.Update();
            
            Assert.AreEqual(2, enteredCount);
            Assert.AreEqual(5, updatingCount);
            Assert.AreEqual(1, exitedCount);
            Assert.AreEqual(1, backgroundModeEnteredCount);
            Assert.AreEqual(1, backgroundUpdatingCount);
            Assert.AreEqual(1, backgroundModeExitedCount);
            
            stateMachine.ForceStop();
            
            Assert.Pass();
        }
        
        [Test]
        public void OnEnter_CalledOnce_PerActivation()
        {
            int enterCount = 0;
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState { Entered = () => ++enterCount };
            
            stateMachine.PushState(state);
            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.AreEqual(1, enterCount);
        }
        
        [Test]
        public void OnExit_CalledOnce_WhenStopped()
        {
            int exitCount = 0;
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState { Exited = () => ++exitCount };
            
            stateMachine.PushState(state);
            stateMachine.Update();
            state.Stop();
            stateMachine.Update();
            
            Assert.AreEqual(1, exitCount);
        }
    }
}

