using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class StateMachineBasicTests
    {
        [Test]
        public void StateMachine_IsIdle_WhenEmpty()
        {
            var stateMachine = new CyclopsStateMachine();
            Assert.IsTrue(stateMachine.IsIdle);
        }
        
        [Test]
        public void StateMachine_IsNotIdle_AfterPushAndUpdate()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.IsFalse(stateMachine.IsIdle);
        }
        
        [Test]
        public void StateMachine_Update_OnEmptyStack_DoesNotThrow()
        {
            var stateMachine = new CyclopsStateMachine();
            Assert.DoesNotThrow(() => stateMachine.Update());
        }
        
        [Test]
        public void StateMachine_Context_ReflectsCurrentState()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.AreSame(state, stateMachine.Context);
        }
    }
}

