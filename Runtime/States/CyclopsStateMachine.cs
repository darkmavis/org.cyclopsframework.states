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

    /// <summary>
    /// <para>Manages a stack of <see cref="CyclopsState"/> instances.</para>
    /// <para>Operates as a classic FSM until states are stacked, at which point it becomes a push-down automata.</para>
    /// <para>For a comprehensive explanation of the state pattern and its relatives, please see:
    /// https://gameprogrammingpatterns.com/state.html</para>
    /// </summary>
    public class CyclopsStateMachine
    {
        private readonly LinkedList<CyclopsState> _stateLinkedStack = new();
        private readonly Queue<CyclopsState> _pushQueue = new();
        private CyclopsState _nextState;
        private bool _isForceStopping;

        // Cache for type names (type -> (fullName, shortName))
        private static readonly Dictionary<Type, (string name, string shortName)> TypeNameCache = new();

        // Cache for transition snapshots per state instance (invalidated if count changes)
        private static readonly Dictionary<CyclopsState, TransitionCache> TransitionSnapshotCache = new();

        // Reusable empty array to avoid allocations
        private static readonly TransitionSnapshot[] EmptyTransitions = Array.Empty<TransitionSnapshot>();

        /// <summary>
        /// The current context state (last state that was updated).
        /// </summary>
        public CyclopsState Context { get; private set; }

        /// <summary>
        /// Whether the state stack is empty.
        /// </summary>
        public bool IsIdle => _stateLinkedStack.Count == 0;

        /// <summary>
        /// Number of states currently on the stack.
        /// </summary>
        public int StateCount => _stateLinkedStack.Count;

        private CyclopsState TopState => _stateLinkedStack.Last.Value;

        /// <summary>
        /// Push a state onto the stack. It will be started on the next Update().
        /// </summary>
        public void PushState(CyclopsState state)
        {
            state.IsForegroundState = true;
            _pushQueue.Enqueue(state);
        }

        /// <summary>
        /// Immediately stop all states and clear the stack.
        /// States are stopped in reverse order (top to bottom).
        /// </summary>
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

        /// <summary>
        /// Update the state machine. Call once per frame.
        /// </summary>
        public void Update()
        {
            while (_pushQueue.TryDequeue(out CyclopsState state))
            {
                _stateLinkedStack.AddLast(state);
            }

            if (IsIdle)
                return;

            CyclopsState topState = TopState;

            foreach (CyclopsState backgroundState in _stateLinkedStack)
            {
                if (backgroundState == topState)
                    continue;

                Context = backgroundState;

                // In case of pushing multiple states onto the stack quickly.
                if (!backgroundState.IsActive)
                {
                    // We'll say this is a foreground state, but it won't be for long.
                    backgroundState.IsForegroundState = true;
                    backgroundState.Start();
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

        // =========== Debug Monitoring API ===========

        /// <summary>
        /// Provides a snapshot of the current state stack for debug visualization.
        /// States are passed bottom-to-top (index 0 is the bottom of the stack).
        /// The list is pooled and only valid for the duration of the callback.
        /// </summary>
        public void WithStateStackSnapshot(Action<IReadOnlyList<StateSnapshot>> action)
        {
            var list = ListPool<StateSnapshot>.Get();
            try
            {
                foreach (CyclopsState state in _stateLinkedStack)
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
        public T WithStateStackSnapshot<T>(Func<IReadOnlyList<StateSnapshot>, T> func)
        {
            var list = ListPool<StateSnapshot>.Get();
            try
            {
                foreach (CyclopsState state in _stateLinkedStack)
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
        public void VisitStateStack(Action<int, StateSnapshot> visitor)
        {
            int index = 0;
            foreach (CyclopsState state in _stateLinkedStack)
            {
                visitor(index++, CreateStateSnapshot(state));
            }
        }

        private StateSnapshot CreateStateSnapshot(CyclopsState state)
        {
            (string name, string shortName) = GetStateNames(state);

            return new StateSnapshot
            {
                Name = name,
                ShortName = shortName,
                IsActive = state.IsActive,
                IsForegroundState = state.IsForegroundState,
                Transitions = GetTransitionSnapshots(state)
            };
        }

        private static (string name, string shortName) GetStateNames(CyclopsState state)
        {
            // If state has a custom Name, use it
            if (!string.IsNullOrEmpty(state.Name))
                return (state.Name, state.Name);

            Type stateType = state.GetType();

            // Check cache
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

        private static IReadOnlyList<TransitionSnapshot> GetTransitionSnapshots(CyclopsState state)
        {
            IReadOnlyList<CyclopsStateTransition> transitions = state.Transitions;
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
                CyclopsState target = t.Target;

                string targetName = null;
                string targetShortName = null;

                if (target != null)
                {
                    (targetName, targetShortName) = GetStateNames(target);
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
