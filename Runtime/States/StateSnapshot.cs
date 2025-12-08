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

using System.Collections.Generic;

namespace Cyclops.States
{
    /// <summary>
    /// Read-only snapshot of a state for debug purposes.
    /// </summary>
    public readonly struct StateSnapshot
    {
        /// <summary>Full name of the state (from attribute or full type name).</summary>
        public string Name { get; init; }
        
        /// <summary>Short name of the state suitable for compact display.</summary>
        public string ShortName { get; init; }
        
        /// <summary>Whether the state has been entered and not yet exited.</summary>
        public bool IsActive { get; init; }
        
        /// <summary>Whether this is the top state on the stack.</summary>
        public bool IsForegroundState { get; init; }
        
        /// <summary>All transitions registered on this state (cached, not allocated per call).</summary>
        public IReadOnlyList<TransitionSnapshot> Transitions { get; init; }
    }
}

