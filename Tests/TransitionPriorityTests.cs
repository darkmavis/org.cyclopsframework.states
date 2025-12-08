using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class TransitionPriorityTests
    {
        [Test]
        public void Transitions_FirstMatch_Wins()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            var stateC = new CyclopsState();
            
            // Both transitions are true, but first one should win
            stateA.AddTransition(stateB, () => true);
            stateA.AddTransition(stateC, () => true);
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsTrue(stateB.IsActive);
            Assert.IsFalse(stateC.IsActive);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void Transitions_NoMatch_StaysInState()
        {
            int updateCount = 0;
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState { Updating = () => ++updateCount };
            var stateB = new CyclopsState();
            
            stateA.AddTransition(stateB, () => false);
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            stateMachine.Update();
            stateMachine.Update();
            
            Assert.IsTrue(stateA.IsActive);
            Assert.IsFalse(stateB.IsActive);
            Assert.AreEqual(3, updateCount);
            
            stateMachine.ForceStop();
        }
    }
}

