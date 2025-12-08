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
        public void Dispose_CanBeCalledAfterStateUsed()
        {
            var stateMachine = new CyclopsStateMachine();
            var state = new CyclopsState();
            
            stateMachine.PushState(state);
            stateMachine.Update();
            stateMachine.ForceStop();
            
            Assert.DoesNotThrow(() => state.Dispose());
        }
        
        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var state = new CyclopsState();
            
            Assert.DoesNotThrow(() =>
            {
                state.Dispose();
                state.Dispose();
                state.Dispose();
            });
        }
    }
}
