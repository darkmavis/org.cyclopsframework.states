using System;
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class ExitTransitionTests
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
        public void AddExitTransition_FiresOnStop()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            bool enteredB = false;
            stateB.Entered = () => enteredB = true;
            
            stateA.AddExitTransition(stateB);
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            Assert.IsTrue(stateA.IsActive);
            Assert.IsFalse(enteredB);
            
            stateA.Stop();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsFalse(stateA.IsActive);
            Assert.IsTrue(enteredB);
        }
        
        [Test]
        public void ExitOnAction_CorrectlyExits()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            state.ExitOnAction(ref _fooAction);
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.IsTrue(state.IsActive);
            
            _fooAction?.Invoke();
            stateMachine.Update();
            
            Assert.IsFalse(state.IsActive);
        }
        
        [Test]
        public void ExitOnAction_Generic_CorrectlyExits()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            state.ExitOnAction(ref _paramAction);
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.IsTrue(state.IsActive);
            
            _paramAction?.Invoke(77);
            stateMachine.Update();
            
            Assert.IsFalse(state.IsActive);
        }
    }
}

