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
    // Test state with custom name attribute
    [CyclopsStateName("My Custom State")]
    public class NamedTestState : CyclopsBaseState { }
    
    // Test state without attribute
    public class UnnamedTestState : CyclopsBaseState { }
    
    // Test generic state
    public class GenericTestState<T> : CyclopsBaseState { }
    
    // Test nested generic state
    public class NestedGenericState<T1, T2> : CyclopsBaseState { }

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
        public void StateWithAttribute_UsesCustomName()
        {
            var fsm = new CyclopsStateMachine();
            var state = new NamedTestState();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("My Custom State", states[0].Name);
                Assert.AreEqual("My Custom State", states[0].ShortName);
            });
        }

        [Test]
        public void StateWithoutAttribute_UsesTypeName()
        {
            var fsm = new CyclopsStateMachine();
            var state = new UnnamedTestState();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("UnnamedTestState", states[0].ShortName);
                Assert.IsTrue(states[0].Name.EndsWith("UnnamedTestState"));
            });
        }

        [Test]
        public void GenericState_FormatsShortNameCorrectly()
        {
            var fsm = new CyclopsStateMachine();
            var state = new GenericTestState<int>();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("GenericTestState<Int32>", states[0].ShortName);
            });
        }

        [Test]
        public void NestedGenericState_FormatsShortNameCorrectly()
        {
            var fsm = new CyclopsStateMachine();
            var state = new NestedGenericState<string, int>();
            fsm.PushState(state);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("NestedGenericState<String,Int32>", states[0].ShortName);
            });
        }

        [Test]
        public void TransitionTarget_WithAttribute_UsesCustomName()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new UnnamedTestState();
            var stateB = new NamedTestState();
            
            stateA.AddTransition(stateB, () => false);
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("My Custom State", states[0].Transitions[0].TargetShortName);
                Assert.AreEqual("My Custom State", states[0].Transitions[0].TargetName);
            });
        }

        [Test]
        public void TransitionTarget_GenericState_FormatsCorrectly()
        {
            var fsm = new CyclopsStateMachine();
            var stateA = new UnnamedTestState();
            var stateB = new GenericTestState<float>();
            
            stateA.AddTransition(stateB, () => false);
            fsm.PushState(stateA);
            fsm.Update();
            
            fsm.WithStateStackSnapshot(states =>
            {
                Assert.AreEqual("GenericTestState<Single>", states[0].Transitions[0].TargetShortName);
            });
        }

        [Test]
        public void TypeNameCache_IsCached()
        {
            var fsm = new CyclopsStateMachine();
            var state = new UnnamedTestState();
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
            var stateA = new NamedTestState();
            var stateB = new UnnamedTestState();
            var stateC = new GenericTestState<string>();
            
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
                Assert.AreEqual("My Custom State", states[0].ShortName);
                Assert.AreEqual("UnnamedTestState", states[1].ShortName);
                Assert.AreEqual("GenericTestState<String>", states[2].ShortName);
            });
        }
    }
}

