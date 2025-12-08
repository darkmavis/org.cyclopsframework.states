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
    public class StateNameAttributeTests
    {
        [SetUp]
        public void SetUp()
        {
            CyclopsStateMachine.ClearMonitoringCaches();
        }
        
        [TearDown]
        public void TearDown()
        {
            CyclopsStateMachine.ClearMonitoringCaches();
        }

        [Test]
        public void StateWithCustomName_UsesCustomName()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState { Name = "My Custom State" };
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("My Custom State", states[0].Name);
                Assert.AreEqual("My Custom State", states[0].ShortName);
            });
        }

        [Test]
        public void StateWithoutCustomName_UsesTypeName()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("CyclopsState", states[0].ShortName);
                Assert.IsTrue(states[0].Name.EndsWith("CyclopsState"));
            });
        }

        [Test]
        public void TransitionTarget_WithCustomName_UsesCustomName()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState { Name = "State A" };
            var stateB = new CyclopsState { Name = "State B" };
            
            stateA.AddTransition(stateB, () => false);
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("State B", states[0].Transitions[0].TargetShortName);
                Assert.AreEqual("State B", states[0].Transitions[0].TargetName);
            });
        }

        [Test]
        public void TypeNameCache_IsCached()
        {
            var fsm = new CyclopsStateMachine();
            var state = new CyclopsState();
            fsm.PushState(state);
            fsm.Update();
            
            string firstName = null;
            string secondName = null;
            
            fsm.WithStateStackSnapshot(states =>
            {
                firstName = states[0].ShortName;
            });
            
            fsm.WithStateStackSnapshot(states =>
            {
                secondName = states[0].ShortName;
            });
            
            // Should be the exact same string instance (cached)
            Assert.AreSame(firstName, secondName);
        }

        [Test]
        public void MixedStates_AllNamesCorrect()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new CyclopsState { Name = "Custom A" };
            var stateB = new CyclopsState(); // Uses type name
            var stateC = new CyclopsState { Name = "Custom C" };
            
            // Set up push chain
            stateA.AddPushTransition(stateB, () => true);
            stateB.AddPushTransition(stateC, () => true);
            
            fsm.PushState(stateA);
            
            // Run through all transitions
            for (int i = 0; i < 6; i++)
                fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual(3, states.Count);
                Assert.AreEqual("Custom A", states[0].ShortName);
                Assert.AreEqual("CyclopsState", states[1].ShortName);
                Assert.AreEqual("Custom C", states[2].ShortName);
            });
        }
    }
}
