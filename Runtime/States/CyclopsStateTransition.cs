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

namespace Cyclops.States
{
    /// <summary>
    /// Represents a transition from one state to another.
    /// </summary>
    public struct CyclopsStateTransition
    {
        /// <summary>
        /// Predicate that determines if this transition should fire.
        /// </summary>
        public Func<bool> Condition { get; set; }

        /// <summary>
        /// Target state to transition to. Null for Pop operations.
        /// </summary>
        public CyclopsState Target { get; set; }

        /// <summary>
        /// Stack operation to perform (Replace, Push, or Pop).
        /// </summary>
        public StackOp Op { get; set; }
    }
}
