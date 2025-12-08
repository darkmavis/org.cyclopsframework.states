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

