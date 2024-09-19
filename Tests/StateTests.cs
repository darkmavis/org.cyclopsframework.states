using System;
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class StateTests
    {
        private Action _fooAction;
        
        [Test]
        public void Test_AddPushTransition_IsPushed()
        {
            CyclopsStateMachine stateMachine = new();
            CyclopsState stateA = new();
            CyclopsState stateB = new();
            stateA.AddPushTransition(stateB, () => true);
            stateMachine.PushState(stateA);
            Assert.IsTrue(stateA.IsForegroundState);
            stateMachine.Update();
            stateMachine.Update();
            Assert.IsFalse(stateA.IsForegroundState);
            Assert.IsTrue(stateB.IsForegroundState);
        }
        
        [Test]
        public void Test_ExitOnAction_CorrectlyExits()
        {
            CyclopsStateMachine stateMachine = new();
            CyclopsState state = new();
            state.ExitOnAction(ref _fooAction);
            stateMachine.PushState(state);
            stateMachine.Update();
            Assert.IsTrue(state.IsActive);
            _fooAction?.Invoke();
            stateMachine.Update();
            Assert.IsFalse(state.IsActive);
        }
        
        [Test]
        public void Update_CyclopsState_CountsAreCorrect()
        {
            int enteredCount = 0;
            int updatingCount = 0;
            int backgroundUpdatingCount = 0;
            int exitedCount = 0;
            int backgroundModeEnteredCount = 0;
            int backgroundModeExitedCount = 0;
            
            CyclopsStateMachine stateMachine = new();
            CyclopsState stateA = new();
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
            
            CyclopsState stateB = new();
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
    }
}