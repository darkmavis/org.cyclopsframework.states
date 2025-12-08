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

