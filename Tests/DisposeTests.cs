using System;
using NUnit.Framework;

namespace Cyclops.States.Tests
{
    public class DisposeTests
    {
        [Test]
        public void Dispose_CanBeCalledSafely()
        {
            var state = new CyclopsState();
            Assert.DoesNotThrow(() => state.Dispose());
        }
        
        [Test]
        public void Dispose_CustomDispose_IsCalled()
        {
            bool customDisposeCalled = false;
            var state = new TestDisposableState(() => customDisposeCalled = true);
            
            state.Dispose();
            
            Assert.IsTrue(customDisposeCalled);
        }
    }
    
    /// <summary>
    /// Helper class to test custom Dispose behavior
    /// </summary>
    public class TestDisposableState : CyclopsBaseState
    {
        private readonly Action _onDispose;
        
        public TestDisposableState(Action onDispose)
        {
            _onDispose = onDispose;
        }
        
        protected override void Dispose(bool isDisposing)
        {
            _onDispose?.Invoke();
        }
    }
}

