using System;
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class ReplaceTransitionTests
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
        public void AddTransition_Replaces_CurrentState()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            bool exitedA = false;
            bool shouldTransition = false;
            stateA.Exited = () => exitedA = true;
            
            stateA.AddTransition(stateB, () => shouldTransition);
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            Assert.IsTrue(stateA.IsActive);
            Assert.IsFalse(exitedA);
            
            // Trigger the transition
            shouldTransition = true;
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsActive);
            Assert.IsTrue(stateB.IsActive);
            Assert.IsTrue(exitedA);
        }
        
        [Test]
        public void AddTransition_ViaAction_Replaces()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, ref _fooAction);
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            Assert.IsTrue(stateA.IsActive);
            
            _fooAction?.Invoke();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsActive);
            Assert.IsTrue(stateB.IsActive);
        }
        
        [Test]
        public void AddTransition_ViaGenericAction_Replaces()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, ref _paramAction);
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            _paramAction?.Invoke(99);
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsActive);
            Assert.IsTrue(stateB.IsActive);
        }
    }
}

