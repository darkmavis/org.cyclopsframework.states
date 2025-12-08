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
    /// Optional attribute to provide a custom display name for a state in debug overlays.
    /// When present, the Name will be used instead of the class name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CyclopsStateNameAttribute : Attribute
    {
        public string Name { get; }
        public CyclopsStateNameAttribute(string name) => Name = name;
    }
}

