using System.Collections.Generic;
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class ForceStopTests
    {
        [Test]
        public void ForceStop_StopsAllStates_InOrder()
        {
            var exitOrder = new List<string>();
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState { Exited = () => exitOrder.Add("A") };
            var stateB = new CyclopsState { Exited = () => exitOrder.Add("B") };
            var stateC = new CyclopsState { Exited = () => exitOrder.Add("C") };
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            stateMachine.PushState(stateC);
            stateMachine.Update();
            
            stateMachine.ForceStop();
            
            // States should exit top-to-bottom (C, B, A)
            Assert.AreEqual(3, exitOrder.Count);
            Assert.AreEqual("C", exitOrder[0]);
            Assert.AreEqual("B", exitOrder[1]);
            Assert.AreEqual("A", exitOrder[2]);
        }
        
        [Test]
        public void ForceStop_MachineBecomesIdle()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            
            Assert.IsFalse(stateMachine.IsIdle);
            
            stateMachine.ForceStop();
            
            Assert.IsTrue(stateMachine.IsIdle);
        }
        
        [Test]
        public void ForceStop_CancelsPendingPushes()
        {
            var stateMachine = new CyclopsStateMachine();
            var stateA = new CyclopsState();
            var stateB = new CyclopsState();
            
            stateMachine.PushState(stateA);
            stateMachine.PushState(stateB);
            // Don't call Update() yet - states are queued
            
            stateMachine.ForceStop();
            
            Assert.IsTrue(stateMachine.IsIdle);
            Assert.IsFalse(stateA.IsActive);
            Assert.IsFalse(stateB.IsActive);
        }
    }
}

