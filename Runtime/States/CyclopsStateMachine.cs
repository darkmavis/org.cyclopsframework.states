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
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Pool;

namespace Cyclops.States
{
    /// <summary>
    /// Cached transition data for a state instance.
    /// </summary>
    internal readonly struct TransitionCache
    {
        public int Count { get; init; }
        public TransitionSnapshot[] Snapshots { get; init; }
    }

    public class CyclopsStateMachine
    {
        private readonly LinkedList<CyclopsBaseState> _stateLinkedStack = new();
        private readonly Queue<CyclopsBaseState> _pushQueue = new();
        private CyclopsBaseState _nextState;
        private bool _isForceStopping;

        // Cached reflection info for accessing private transitions
        private static readonly FieldInfo TransitionsField =
            typeof(CyclopsBaseState).GetField("_transitions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Cache for type names (type -> (fullName, shortName))
        private static readonly Dictionary<Type, (string name, string shortName)> TypeNameCache = new();

        // Cache for transition snapshots per state instance (invalidated if count changes)
        private static readonly Dictionary<CyclopsBaseState, TransitionCache> TransitionSnapshotCache = new();

        // Reusable empty array to avoid allocations
        private static readonly TransitionSnapshot[] EmptyTransitions = Array.Empty<TransitionSnapshot>();

        public CyclopsBaseState Context { get; private set; }
        public bool IsIdle => _stateLinkedStack.Count == 0;

        /// <summary>
        /// Number of states currently on the stack.
        /// </summary>
        public int StateCount => _stateLinkedStack.Count;

        private CyclopsBaseState TopState => _stateLinkedStack.Last.Value;

        public void PushState(CyclopsBaseState state)
        {
            state.IsForegroundState = true;
            _pushQueue.Enqueue(state);
        }

        public void ForceStop()
        {
            _pushQueue.Clear();

            while (_stateLinkedStack.Count != 0)
            {
                TopState.StopImmediately();
                _stateLinkedStack.RemoveLast();
            }

            _isForceStopping = true;
        }

        public void Update()
        {
            while (_pushQueue.TryDequeue(out CyclopsBaseState state))
            {
                _stateLinkedStack.AddLast(state);
            }

            if (IsIdle)
                return;

            CyclopsBaseState topState = TopState;

            foreach (CyclopsBaseState backgroundState in _stateLinkedStack)
            {
                if (backgroundState == topState)
                    continue;

                // Could set this to null later, but would rather not.
                Context = backgroundState;

                // In case of pushing multiple states onto the stack quickly.
                if (!backgroundState.IsActive)
                {
                    // We'll say this is a foreground state, but it won't be for long.
                    backgroundState.IsForegroundState = true;
                    backgroundState.Start(); // <-- calls: OnEnter
                }

                if (backgroundState.IsForegroundState)
                {
                    backgroundState.IsForegroundState = false;
                    backgroundState.JustEnteredBackgroundMode = true;
                }

                backgroundState.Update();

                if (_isForceStopping)
                    return;
            }

            Context = topState;

            if (!topState.IsActive)
                topState.Start();

            if (topState.QueryTransitions(out _nextState, out StackOp op))
            {
                if (op is StackOp.Replace or StackOp.Pop)
                    topState.Stop();
            }
            else
            {
                if (!topState.IsForegroundState)
                {
                    topState.IsForegroundState = true;
                    topState.JustExitedBackgroundMode = true;
                }

                topState.Update();

                if (topState.QueryTransitions(out _nextState, out op))
                {
                    if (op is StackOp.Replace or StackOp.Pop)
                        topState.Stop();
                }
            }

            if (topState.IsStopping)
            {
                topState.StopImmediately();
                _stateLinkedStack.RemoveLast();

                // Immediately promote the new top state to foreground.
                // This ensures IsForegroundState is always accurate after a pop,
                // not delayed until the next Update() cycle.
                if (!IsIdle)
                {
                    TopState.IsForegroundState = true;
                    TopState.JustExitedBackgroundMode = true;
                }
            }

            if (_nextState is null)
                return;

            PushState(_nextState);
        }
        
        /// <summary>
        /// Provides a snapshot of the current state stack for debug visualization.
        /// States are passed bottom-to-top (index 0 is the bottom of the stack).
        /// The list is pooled and only valid for the duration of the callback.
        /// </summary>
        /// <param name="action">Action to perform with the snapshot list.</param>
        public void WithStateStackSnapshot(Action<IReadOnlyList<StateSnapshot>> action)
        {
            var list = ListPool<StateSnapshot>.Get();
            try
            {
                foreach (CyclopsBaseState state in _stateLinkedStack)
                    list.Add(CreateStateSnapshot(state));
                
                action(list);
            }
            finally
            {
                ListPool<StateSnapshot>.Release(list);
            }
        }
        
        /// <summary>
        /// Provides a snapshot of the current state stack and returns a computed result.
        /// States are passed bottom-to-top (index 0 is the bottom of the stack).
        /// The list is pooled and only valid for the duration of the callback.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="func">Function to compute a result from the snapshot list.</param>
        /// <returns>The computed result.</returns>
        public T WithStateStackSnapshot<T>(Func<IReadOnlyList<StateSnapshot>, T> func)
        {
            var list = ListPool<StateSnapshot>.Get();
            try
            {
                foreach (CyclopsBaseState state in _stateLinkedStack)
                    list.Add(CreateStateSnapshot(state));
                
                return func(list);
            }
            finally
            {
                ListPool<StateSnapshot>.Release(list);
            }
        }
        
        /// <summary>
        /// Enumerates the state stack from bottom to top, invoking the callback for each state.
        /// Zero-allocation after initial cache warmup.
        /// </summary>
        /// <param name="visitor">Callback invoked for each state with its index (0 = bottom).</param>
        public void VisitStateStack(Action<int, StateSnapshot> visitor)
        {
            int index = 0;
            foreach (CyclopsBaseState state in _stateLinkedStack)
            {
                visitor(index++, CreateStateSnapshot(state));
            }
        }
        
        private StateSnapshot CreateStateSnapshot(CyclopsBaseState state)
        {
            (string name, string shortName) = GetStateNames(state.GetType());
            
            return new StateSnapshot
            {
                Name = name,
                ShortName = shortName,
                IsActive = state.IsActive,
                IsForegroundState = state.IsForegroundState,
                Transitions = GetTransitionSnapshots(state)
            };
        }
        
        private static (string name, string shortName) GetStateNames(Type stateType)
        {
            // Check cache first
            if (TypeNameCache.TryGetValue(stateType, out var cached))
                return cached;
            
            // Check for custom attribute
            var attr = stateType.GetCustomAttribute<CyclopsStateNameAttribute>();
            if (attr != null)
            {
                var result = (attr.Name, attr.Name);
                TypeNameCache[stateType] = result;
                return result;
            }
            
            // Derive from type name
            string fullName = stateType.FullName ?? stateType.Name;
            string shortName = GetShortTypeName(stateType);
            
            var names = (fullName, shortName);
            TypeNameCache[stateType] = names;
            return names;
        }
        
        private static string GetShortTypeName(Type type)
        {
            string name = type.Name;
            
            // Handle generic types: MyState`1 -> MyState<T>
            int backtickIndex = name.IndexOf('`');
            if (backtickIndex > 0 && type.IsGenericType)
            {
                string baseName = name[..backtickIndex];
                Type[] args = type.GetGenericArguments();
                string argsStr = string.Join(",", Array.ConvertAll(args, t => GetShortTypeName(t)));
                return $"{baseName}<{argsStr}>";
            }
            
            return name;
        }
        
        private static IReadOnlyList<TransitionSnapshot> GetTransitionSnapshots(CyclopsBaseState state)
        {
            // Access private _transitions field via reflection
            if (TransitionsField?.GetValue(state) is not List<CyclopsStateTransition> transitions)
                return EmptyTransitions;
            
            int currentCount = transitions.Count;
            
            // Check cache - invalidate if transition count changed
            if (TransitionSnapshotCache.TryGetValue(state, out TransitionCache cached))
            {
                if (cached.Count == currentCount)
                    return cached.Snapshots;
            }
            
            // Build new cache entry
            if (currentCount == 0)
            {
                TransitionSnapshotCache[state] = new TransitionCache { Count = 0, Snapshots = EmptyTransitions };
                return EmptyTransitions;
            }
            
            var snapshots = new TransitionSnapshot[currentCount];
            
            for (int i = 0; i < currentCount; i++)
            {
                CyclopsStateTransition t = transitions[i];
                CyclopsBaseState target = t.Target;
                
                string targetName = null;
                string targetShortName = null;
                
                if (target != null)
                {
                    (targetName, targetShortName) = GetStateNames(target.GetType());
                }
                
                snapshots[i] = new TransitionSnapshot
                {
                    Op = t.Op,
                    TargetName = targetName,
                    TargetShortName = targetShortName
                };
            }
            
            TransitionSnapshotCache[state] = new TransitionCache { Count = currentCount, Snapshots = snapshots };
            return snapshots;
        }
        
        /// <summary>
        /// Clears all debug monitoring caches. Call this if you're done with debug visualization
        /// and want to free memory, or if you've disposed states that were previously monitored.
        /// </summary>
        public static void ClearMonitoringCaches()
        {
            TypeNameCache.Clear();
            TransitionSnapshotCache.Clear();
        }
    }
}
