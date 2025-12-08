using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class BackgroundModeTests
    {
        [Test]
        public void BackgroundState_ReceivesBackgroundUpdates()
        {
            int backgroundUpdates = 0;
            int foregroundUpdates = 0;
            
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState
            {
                Updating = () => ++foregroundUpdates,
                BackgroundUpdating = () => ++backgroundUpdates
            };
            var stateB = new CyclopsState();
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            Assert.AreEqual(1, foregroundUpdates);
            Assert.AreEqual(0, backgroundUpdates);
            
            stateMachine.PushState(stateB);
            stateMachine.Update();
            
            Assert.AreEqual(1, foregroundUpdates);
            Assert.AreEqual(1, backgroundUpdates);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void BackgroundModeEntered_CalledOnPush()
        {
            bool enteredBackground = false;
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState
            {
                BackgroundModeEntered = () => enteredBackground = true
            };
            var stateB = new CyclopsState();
            
            stateMachine.PushState(stateA);
            stateMachine.Update();
            
            Assert.IsFalse(enteredBackground);
            
            stateMachine.PushState(stateB);
            stateMachine.Update();
            
            Assert.IsTrue(enteredBackground);
            
            stateMachine.ForceStop();
        }
        
        [Test]
        public void BackgroundModeExited_CalledOnPop()
        {
            bool exitedBackground = false;
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState
            {
                BackgroundModeExited = () => exitedBackground = true
            };
            var stateB = new CyclopsState();
            
            stateB.AddPopTransition(() => true);
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.Update();
            
            Assert.IsFalse(exitedBackground);
            
            stateMachine.Update(); // stateB pops
            
            Assert.IsTrue(exitedBackground);
            
            stateMachine.ForceStop();
        }
    }
}

