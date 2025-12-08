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
using System.Threading;
using UnityEngine.Pool;

namespace Cyclops.States
{
    /// <summary>
    /// Result status for behavior tree-style state evaluation.
    /// </summary>
    public enum BtResult
    {
        /// <summary>State is still executing.</summary>
        Running,
        /// <summary>State completed successfully.</summary>
        Success,
        /// <summary>State completed with failure.</summary>
        Failure
    }

    /// <summary>
    /// <para>Unified state class for FSM and behavior tree usage.</para>
    /// <para>For FSM usage: set lifecycle hooks (<see cref="OnEnter"/>, <see cref="OnUpdate"/>, etc.)
    /// and wire transitions with <see cref="AddTransition"/>.</para>
    /// <para>For BT usage: call <see cref="Succeed"/> or <see cref="Fail"/> to report results,
    /// then use <see cref="OnSuccess"/> or <see cref="OnFailure"/> for result-based transitions.</para>
    /// <para>For a comprehensive explanation of the state pattern and its relatives, please see:
    /// https://gameprogrammingpatterns.com/state.html</para>
    /// <seealso cref="CyclopsStateMachine"/>
    /// </summary>
    public sealed class CyclopsState : IDisposable
    {
        private readonly List<CyclopsStateTransition> _transitions = ListPool<CyclopsStateTransition>.Get();
        private CancellationTokenSource _exitCancellationTokenSource;
        private bool _isStopping;
        private bool _isDisposed;

        // =========== Lifecycle Hooks ===========

        /// <summary>
        /// Invoked when this state is entered. A state cannot be entered again until after it exits.
        /// </summary>
        public Action OnEnter { get; set; }

        /// <summary>
        /// Invoked when this state is exited. A state cannot exit until after it is entered.
        /// </summary>
        public Action OnExit { get; set; }

        /// <summary>
        /// Invoked each frame when this state is the foreground (top) state on the stack.
        /// </summary>
        public Action OnUpdate { get; set; }

        /// <summary>
        /// Invoked each frame when this state is on the stack but not the foreground state.
        /// </summary>
        public Action OnBackgroundUpdate { get; set; }

        /// <summary>
        /// Invoked when this state transitions from foreground to background.
        /// </summary>
        public Action OnEnterBackground { get; set; }

        /// <summary>
        /// Invoked when this state transitions from background to foreground.
        /// </summary>
        public Action OnExitBackground { get; set; }

        // =========== State Properties ===========

        /// <summary>
        /// Optional name for debugging and visualization.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Cancellation token that is cancelled when this state exits.
        /// Use for async operations that should be cancelled when the state ends.
        /// </summary>
        public CancellationToken ExitCancellationToken { get; private set; }

        /// <summary>
        /// Whether this state has been entered and not yet exited.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Whether this state is the top state on the stack.
        /// </summary>
        public bool IsForegroundState { get; internal set; }

        /// <summary>
        /// Behavior tree result status. Defaults to <see cref="BtResult.Running"/>.
        /// Set via <see cref="Succeed"/> or <see cref="Fail"/>.
        /// </summary>
        public BtResult Result { get; private set; } = BtResult.Running;

        // Internal flags for background mode transitions
        internal bool JustEnteredBackgroundMode { get; set; }
        internal bool JustExitedBackgroundMode { get; set; }
        internal bool IsStopping => _isStopping;

        // =========== Lifecycle (called by state machine) ===========

        /// <summary>
        /// Called by the host state machine. Do not call directly.
        /// </summary>
        internal void Start()
        {
            _exitCancellationTokenSource = new CancellationTokenSource();
            ExitCancellationToken = _exitCancellationTokenSource.Token;
            IsActive = true;
            _isStopping = false;
            Result = BtResult.Running;

            OnEnter?.Invoke();
        }

        /// <summary>
        /// Called by the host state machine. Do not call directly.
        /// </summary>
        internal void Update()
        {
            if (JustEnteredBackgroundMode)
            {
                OnEnterBackground?.Invoke();
                JustEnteredBackgroundMode = false;
            }

            if (JustExitedBackgroundMode)
            {
                OnExitBackground?.Invoke();
                JustExitedBackgroundMode = false;
            }

            if (IsForegroundState)
                OnUpdate?.Invoke();
            else
                OnBackgroundUpdate?.Invoke();
        }

        /// <summary>
        /// Called by the host state machine. Do not call directly.
        /// </summary>
        internal void StopImmediately()
        {
            bool wasActive = IsActive;

            IsActive = false;
            _exitCancellationTokenSource?.Cancel();
            _exitCancellationTokenSource?.Dispose();
            _exitCancellationTokenSource = null;

            if (wasActive)
                OnExit?.Invoke();
        }

        /// <summary>
        /// Queries transitions and returns the first matching transition.
        /// Called by the host state machine. Do not call directly.
        /// </summary>
        internal bool QueryTransitions(out CyclopsState nextState, out StackOp op)
        {
            nextState = null;
            op = StackOp.Replace;

            foreach (CyclopsStateTransition transition in _transitions)
            {
                if (!transition.Condition())
                    continue;

                nextState = transition.Target;
                op = transition.Op;
                return true;
            }

            return false;
        }

        // =========== Public API ===========

        /// <summary>
        /// Request this state to stop. The state will exit on the next update cycle.
        /// </summary>
        public void Stop() => _isStopping = true;

        /// <summary>
        /// Mark this state as succeeded and request stop. For behavior tree usage.
        /// </summary>
        public void Succeed()
        {
            Result = BtResult.Success;
            Stop();
        }

        /// <summary>
        /// Mark this state as failed and request stop. For behavior tree usage.
        /// </summary>
        public void Fail()
        {
            Result = BtResult.Failure;
            Stop();
        }

        // =========== Transition API ===========

        /// <summary>
        /// Add a transition with the specified target, condition, and stack operation.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions cannot be removed once added.
        /// </summary>
        /// <param name="target">Target state (null for Pop operations).</param>
        /// <param name="condition">Predicate that triggers the transition when true.</param>
        /// <param name="op">Stack operation to perform.</param>
        /// <returns>This state for fluent chaining.</returns>
        public CyclopsState AddTransition(CyclopsState target, Func<bool> condition, StackOp op = StackOp.Replace)
        {
            _transitions.Add(new CyclopsStateTransition
            {
                Target = target,
                Condition = condition,
                Op = op
            });
            return this;
        }

        /// <summary>
        /// Add a transition that replaces this state with the target when the condition is true.
        /// </summary>
        public CyclopsState AddTransition(CyclopsState target, Func<bool> condition)
            => AddTransition(target, condition, StackOp.Replace);

        /// <summary>
        /// Add a transition that pushes the target state above this state on the stack.
        /// </summary>
        public CyclopsState AddPushTransition(CyclopsState target, Func<bool> condition)
            => AddTransition(target, condition, StackOp.Push);

        /// <summary>
        /// Add a transition that pops this state off the stack.
        /// </summary>
        public CyclopsState AddPopTransition(Func<bool> condition)
            => AddTransition(null, condition, StackOp.Pop);

        /// <summary>
        /// Add a transition that fires when this state is stopping (after Stop() is called).
        /// </summary>
        public CyclopsState AddExitTransition(CyclopsState target)
            => AddTransition(target, () => _isStopping || !IsActive);

        // =========== BT-aware Transitions ===========

        /// <summary>
        /// Add a transition that fires when this state succeeds.
        /// </summary>
        public CyclopsState OnSuccess(CyclopsState target)
            => AddTransition(target, () => Result == BtResult.Success);

        /// <summary>
        /// Add a transition that fires when this state fails.
        /// </summary>
        public CyclopsState OnFailure(CyclopsState target)
            => AddTransition(target, () => Result == BtResult.Failure);

        /// <summary>
        /// Add a transition that fires when this state completes (success or failure).
        /// </summary>
        public CyclopsState OnComplete(CyclopsState target)
            => AddTransition(target, () => Result != BtResult.Running);

        // =========== Internal Access for Monitoring ===========

        /// <summary>
        /// Access transitions for debug monitoring. Do not modify.
        /// </summary>
        internal IReadOnlyList<CyclopsStateTransition> Transitions => _transitions;

        // =========== Dispose ===========

        /// <summary>
        /// Releases internally allocated objects to the pool.
        /// Can be called multiple times safely.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            ListPool<CyclopsStateTransition>.Release(_transitions);
            _exitCancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
