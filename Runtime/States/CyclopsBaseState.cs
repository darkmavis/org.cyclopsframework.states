// Cyclops States
// 
// Copyright 2024 Mark Davis
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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace Cyclops.States
{
    /// <summary>
    /// <para><see cref="CyclopsBaseState"/> operates as a state within a classic FSM until states are stacked.</para>
    /// <para>If states are stacked, then <see cref="CyclopsBaseState"/> operates as a state within a push-down automata.
    /// This works because these patterns are compatible and the usage defines the pattern.</para>
    /// <para>For a comprehensive explanation of the state pattern and its relatives, please see:
    /// https://gameprogrammingpatterns.com/state.html</para>
    /// <seealso cref="CyclopsStateMachine"/>
    /// </summary>
    public abstract class CyclopsBaseState : IDisposable
    {
        private readonly List<CyclopsStateTransition> _transitions = ListPool<CyclopsStateTransition>.Get();
        private CancellationTokenSource _exitCancellationTokenSource = new();

        // ReSharper disable once MemberCanBePrivate.Global
        public CancellationToken ExitCancellationToken { get; private set; }
        public bool IsActive { get; private set; }
        internal bool IsStopping { get; private set; }
        public bool IsForegroundState { get; internal set; }
        internal bool JustEnteredBackgroundMode { get; set; }
        internal bool JustExitedBackgroundMode { get; set; }
        
        /// <summary>
        /// <see cref="Start"/> is called by the host state machine and should not be otherwise called.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        internal void Start()
        {
            _exitCancellationTokenSource ??= new CancellationTokenSource();
            ExitCancellationToken = _exitCancellationTokenSource.Token;
            IsActive = true;
            IsStopping = false;
            
            OnEnter();
        }
        
        /// <summary>
        /// <see cref="Update"/> is called by the host state machine and should not be otherwise called.
        /// See <see cref="CyclopsState"/> for example usage. 
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        internal virtual void Update()
        {
            if (JustEnteredBackgroundMode)
            {
                OnEnterBackgroundMode();
                JustEnteredBackgroundMode = false;
            }
            
            // Probably could be an else if, but if the state machine changed, this could be safer.
            if (JustExitedBackgroundMode)
            {
                OnExitBackgroundMode();
                JustExitedBackgroundMode = false;
            }
            
            if (IsForegroundState)
                OnUpdate();
            else
                OnBackgroundUpdate();
        }
        
        internal void StopImmediately()
        {
            //Debug.Log($"Immediately Stopping State: {Name}");
            bool wasActive = IsActive;
            
            IsActive = false;
            _exitCancellationTokenSource.Cancel();
            _exitCancellationTokenSource.Dispose();
            _exitCancellationTokenSource = null;
            
            if (wasActive)
                OnExit();
        }

        /// <summary>
        /// <see cref="Stop"/> will cause the state to exit if it was entered.
        /// This will also pop a stacked state off the stack.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        public void Stop()
        {
            //Debug.Log($"Stopping State: {Name}");
            IsStopping = true;
        }
        
        /// <summary>
        /// Add a transition from this state to a target state based on a condition.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        /// <param name="transition">transition to add</param>
        public void AddTransition(CyclopsStateTransition transition)
        {
            _transitions.Add(transition);
        }

        /// <summary>
        /// Add a transition from this state to a target state based on a condition.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="predicate">trigger condition</param>
        public void AddTransition(CyclopsBaseState target, Func<bool> predicate)
        {
            AddTransition(new CyclopsStateTransition { Target = target, Condition = predicate });
        }
        
        /// <summary>
        /// Add a transition that pushes the target state above this state on the stack based on a condition.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="predicate">trigger condition</param>
        public void AddPushTransition(CyclopsBaseState target, Func<bool> predicate)
        {
            AddTransition(new CyclopsStateTransition
            {
                Target = target,
                Condition = predicate,
                Op = StackOp.Push
            });
        }
        
        /// <summary>
        /// Add a transition that pops this state off the stack based on a condition.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="predicate">trigger condition</param>
        public void AddPopTransition(Func<bool> predicate)
        {
            AddTransition(new CyclopsStateTransition
            {
                Target = null,
                Condition = predicate,
                Op = StackOp.Pop
            });
        }

        /// <summary>
        /// Add an exit transition from this state to a target state that occurs when this state has stopped.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        /// <param name="target">target state</param>
        public void AddExitTransition(CyclopsBaseState target)
        {
            AddTransition(new CyclopsStateTransition { Target = target, Condition = () => IsStopping || !IsActive });
        }
        
        /// <summary>
        /// <see cref="QueryTransitions"/> is called by the hosting state machine and should not be otherwise called.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        /// <param name="nextBaseState">next target state</param>
        /// <param name="StackOp">stack operation (push, pop, replace)</param>
        internal bool QueryTransitions(out CyclopsBaseState nextBaseState, out StackOp op)
        {
            nextBaseState = null;
            op = StackOp.Replace;

            foreach (CyclopsStateTransition transition in _transitions)
            {
                if (!transition.Condition())
                    continue;
                
                nextBaseState = transition.Target;
                op = transition.Op;

                return true;
            }

            return false;
        }

        /// <summary>
        /// <see cref="OnEnter"/> is invoked when this state is entered.
        /// A state can not be entered again until after it is exited.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        protected virtual void OnEnter() { }
        
        /// <summary>
        /// <see cref="OnExit"/> is invoked once when this state is exited.
        /// A state can not be exited until after it is entered.
        /// A state must be re-entered to exit again.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        protected virtual void OnExit() { }
        
        /// <summary>
        /// <see cref="OnUpdate"/> is invoked when this state is the active state in the state machine's stack.
        /// If this is the only state, then <see cref="OnUpdate"/> will always be invoked.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        protected virtual void OnUpdate() { }
        
        /// <summary>
        /// <see cref="OnBackgroundUpdate"/> is invoked when this state IS NOT the active state in the state machine's stack.
        /// If this is the only state, then <see cref="OnBackgroundUpdate"/> will NEVER be invoked.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        protected virtual void OnBackgroundUpdate() { }
        
        /// <summary>
        /// <see cref="OnEnterBackgroundMode"/> is invoked each time this state becomes a background state.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        protected virtual void OnEnterBackgroundMode() { }
        
        /// <summary>
        /// <see cref="OnEnterBackgroundMode"/> is invoked each time this state becomes a foreground state again.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        protected virtual void OnExitBackgroundMode() { }
        
        /// <summary>
        /// Only use this if you need it. The base class uses <see cref="Dispose"/> to release internally allocated objects to a pool.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        protected virtual void Dispose(bool isDisposing) { }
        
        /// <summary>
        /// <see cref="Dispose"/> releases internally allocated objects to a pool.
        /// <seealso cref="CyclopsStateMachine"/>
        /// </summary>
        public void Dispose()
        {
            ListPool<CyclopsStateTransition>.Release(_transitions);
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        public async Awaitable WaitForSecondsAsync(float seconds)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }
        
        public async Awaitable FixedUpdateAsync()
        {
            try
            {
                await Awaitable.FixedUpdateAsync(ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }
        
        public async Awaitable FromAsyncOperation(AsyncOperation op)
        {
            try
            {
                await Awaitable.FromAsyncOperation(op, ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }
        
        public async Awaitable NextFrameAsync()
        {
            try
            {
                await Awaitable.NextFrameAsync(ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }
        
        public async Awaitable EndOfFrameAsync()
        {
            try
            {
                await Awaitable.EndOfFrameAsync(ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }

        /// <summary>
        /// Add a transition from this state to the target state that fires when the action is invoked.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddTransition(CyclopsBaseState target, ref Action multicastDelegate)
        {
            Action localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition { Target = target, Condition = () => wasTriggered} );
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction()
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition from this state to the target state that fires when the action is invoked.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddTransition<T>(CyclopsBaseState target, ref Action<T> multicastDelegate)
        {
            Action<T> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition { Target = target, Condition = () => wasTriggered} );
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T x)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition from this state to the target state that fires when the action is invoked.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddTransition<T1, T2>(CyclopsBaseState target, ref Action<T1, T2> multicastDelegate)
        {
            Action<T1, T2> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition { Target = target, Condition = () => wasTriggered} );
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition from this state to the target state that fires when the action is invoked.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddTransition<T1, T2, T3>(CyclopsBaseState target, ref Action<T1, T2, T3> multicastDelegate)
        {
            Action<T1, T2, T3> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition { Target = target, Condition = () => wasTriggered} );
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition from this state to the target state that fires when the action is invoked.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddTransition<T1, T2, T3, T4>(CyclopsBaseState target, ref Action<T1, T2, T3, T4> multicastDelegate)
        {
            Action<T1, T2, T3, T4> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition { Target = target, Condition = () => wasTriggered} );
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z, T4 w)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }

        /// <summary>
        /// Add a transition that pushes the target state above this state on the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPushTransition(CyclopsBaseState target, ref Action multicastDelegate)
        {
            Action localMulticastDelegate = null;
            bool wasTriggered = false;

            AddTransition(new CyclopsStateTransition
            {
                Target = target,
                Condition = () => wasTriggered,
                Op = StackOp.Push
            });

            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction()
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }

        /// <summary>
        /// Add a transition that pushes the target state above this state on the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPushTransition<T>(CyclopsBaseState target, ref Action<T> multicastDelegate)
        {
            Action<T> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = target,
                Condition = () => wasTriggered,
                Op = StackOp.Push
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T x)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition that pushes the target state above this state on the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPushTransition<T1, T2>(CyclopsBaseState target, ref Action<T1, T2> multicastDelegate)
        {
            Action<T1, T2> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = target,
                Condition = () => wasTriggered,
                Op = StackOp.Push
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition that pushes the target state above this state on the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPushTransition<T1, T2, T3>(CyclopsBaseState target, ref Action<T1, T2, T3> multicastDelegate)
        {
            Action<T1, T2, T3> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = target,
                Condition = () => wasTriggered,
                Op = StackOp.Push
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition that pushes the target state above this state on the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="target">target state</param>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPushTransition<T1, T2, T3, T4>(CyclopsBaseState target, ref Action<T1, T2, T3, T4> multicastDelegate)
        {
            Action<T1, T2, T3, T4> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = target,
                Condition = () => wasTriggered,
                Op = StackOp.Push
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z, T4 w)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }

        /// <summary>
        /// Add a transition that pops this state off the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPopTransition(ref Action multicastDelegate)
        {
            Action localMulticastDelegate = null;
            bool wasTriggered = false;

            AddTransition(new CyclopsStateTransition
            {
                Target = null,
                Condition = () => wasTriggered,
                Op = StackOp.Pop
            });

            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction()
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition that pops this state off the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPopTransition<T>(ref Action<T> multicastDelegate)
        {
            Action<T> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = null,
                Condition = () => wasTriggered,
                Op = StackOp.Pop
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T x)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition that pops this state off the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPopTransition<T1, T2>(ref Action<T1, T2> multicastDelegate)
        {
            Action<T1, T2> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = null,
                Condition = () => wasTriggered,
                Op = StackOp.Pop
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition that pops this state off the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPopTransition<T1, T2, T3>(ref Action<T1, T2, T3> multicastDelegate)
        {
            Action<T1, T2, T3> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = null,
                Condition = () => wasTriggered,
                Op = StackOp.Pop
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Add a transition that pops this state off the stack.
        /// If multiple transitions match, the first added takes priority.
        /// Transitions can not be removed, nor should they be.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void AddPopTransition<T1, T2, T3, T4>(ref Action<T1, T2, T3, T4> multicastDelegate)
        {
            Action<T1, T2, T3, T4> localMulticastDelegate = null;
            bool wasTriggered = false;
            
            AddTransition(new CyclopsStateTransition
            {
                Target = null,
                Condition = () => wasTriggered,
                Op = StackOp.Pop
            });
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z, T4 w)
            {
                wasTriggered = true;
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
            }
        }
        
        /// <summary>
        /// Exit state when a multicastDelegate is invoked.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void ExitOnAction(ref Action multicastDelegate)
        {
            Action localMulticastDelegate = null;
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction()
            {
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
                Stop();
            }
        }
        
        /// <summary>
        /// Exit state when a multicastDelegate is invoked.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void ExitOnAction<T>(ref Action<T> multicastDelegate)
        {
            Action<T> localMulticastDelegate = null;
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T x)
            {
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
                Stop();
            }
        }
        
        /// <summary>
        /// Exit state when a multicastDelegate is invoked.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void ExitOnAction<T1, T2>(ref Action<T1, T2> multicastDelegate)
        {
            Action<T1, T2> localMulticastDelegate = null;
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y)
            {
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
                Stop();
            }
        }
        
        /// <summary>
        /// Exit state when a multicastDelegate is invoked.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void ExitOnAction<T1, T2, T3>(ref Action<T1, T2, T3> multicastDelegate)
        {
            Action<T1, T2, T3> localMulticastDelegate = null;
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z)
            {
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
                Stop();
            }
        }
        
        /// <summary>
        /// Exit state when a multicastDelegate is invoked.
        /// </summary>
        /// <param name="multicastDelegate">trigger action</param>
        public void ExitOnAction<T1, T2, T3, T4>(ref Action<T1, T2, T3, T4> multicastDelegate)
        {
            Action<T1, T2, T3, T4> localMulticastDelegate = null;
            
            multicastDelegate += OnAction;
            localMulticastDelegate = multicastDelegate;
            Assert.IsNotNull(localMulticastDelegate, "Multicast delegate must not be null.");

            return;
            
            void OnAction(T1 x, T2 y, T3 z, T4 w)
            {
                // ReSharper disable once AccessToModifiedClosure
                localMulticastDelegate -= OnAction;
                Stop();
            }
        }
    }
}
