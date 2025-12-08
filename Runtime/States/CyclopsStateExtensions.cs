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
using UnityEngine;
using UnityEngine.Assertions;

namespace Cyclops.States
{
    /// <summary>
    /// Extension methods for <see cref="CyclopsState"/> providing async helpers
    /// and delegate-based transition wiring.
    /// </summary>
    public static class CyclopsStateExtensions
    {
        // =========== Async Helpers ===========
        // These methods wrap Unity Awaitables with the state's ExitCancellationToken,
        // ensuring async operations are cancelled when the state exits.

        /// <summary>
        /// Wait for the specified number of seconds. Cancelled when state exits.
        /// </summary>
        public static async Awaitable WaitForSecondsAsync(this CyclopsState state, float seconds)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, state.ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Silently cancelled - state exited
            }
        }

        /// <summary>
        /// Wait until the next frame. Cancelled when state exits.
        /// </summary>
        public static async Awaitable NextFrameAsync(this CyclopsState state)
        {
            try
            {
                await Awaitable.NextFrameAsync(state.ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Silently cancelled - state exited
            }
        }

        /// <summary>
        /// Wait until end of frame. Cancelled when state exits.
        /// </summary>
        public static async Awaitable EndOfFrameAsync(this CyclopsState state)
        {
            try
            {
                await Awaitable.EndOfFrameAsync(state.ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Silently cancelled - state exited
            }
        }

        /// <summary>
        /// Wait until next fixed update. Cancelled when state exits.
        /// </summary>
        public static async Awaitable FixedUpdateAsync(this CyclopsState state)
        {
            try
            {
                await Awaitable.FixedUpdateAsync(state.ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Silently cancelled - state exited
            }
        }

        /// <summary>
        /// Wait for an async operation to complete. Cancelled when state exits.
        /// </summary>
        public static async Awaitable FromAsyncOperation(this CyclopsState state, AsyncOperation op)
        {
            try
            {
                await Awaitable.FromAsyncOperation(op, state.ExitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Silently cancelled - state exited
            }
        }

        // =========== Delegate-Based Transitions ===========
        // These methods wire transitions to fire when an Action delegate is invoked.

        /// <summary>
        /// Add a transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddTransition(this CyclopsState state, CyclopsState target, ref Action trigger)
        {
            Action localTrigger = null;
            bool fired = false;

            void Handler()
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddTransition(target, () => fired);
        }

        /// <summary>
        /// Add a transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddTransition<T>(this CyclopsState state, CyclopsState target, ref Action<T> trigger)
        {
            Action<T> localTrigger = null;
            bool fired = false;

            void Handler(T _)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddTransition(target, () => fired);
        }

        /// <summary>
        /// Add a transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddTransition<T1, T2>(this CyclopsState state, CyclopsState target, ref Action<T1, T2> trigger)
        {
            Action<T1, T2> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddTransition(target, () => fired);
        }

        /// <summary>
        /// Add a transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddTransition<T1, T2, T3>(this CyclopsState state, CyclopsState target, ref Action<T1, T2, T3> trigger)
        {
            Action<T1, T2, T3> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __, T3 ___)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddTransition(target, () => fired);
        }

        /// <summary>
        /// Add a transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddTransition<T1, T2, T3, T4>(this CyclopsState state, CyclopsState target, ref Action<T1, T2, T3, T4> trigger)
        {
            Action<T1, T2, T3, T4> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __, T3 ___, T4 ____)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddTransition(target, () => fired);
        }

        // =========== Delegate-Based Push Transitions ===========

        /// <summary>
        /// Add a push transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPushTransition(this CyclopsState state, CyclopsState target, ref Action trigger)
        {
            Action localTrigger = null;
            bool fired = false;

            void Handler()
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPushTransition(target, () => fired);
        }

        /// <summary>
        /// Add a push transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPushTransition<T>(this CyclopsState state, CyclopsState target, ref Action<T> trigger)
        {
            Action<T> localTrigger = null;
            bool fired = false;

            void Handler(T _)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPushTransition(target, () => fired);
        }

        /// <summary>
        /// Add a push transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPushTransition<T1, T2>(this CyclopsState state, CyclopsState target, ref Action<T1, T2> trigger)
        {
            Action<T1, T2> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPushTransition(target, () => fired);
        }

        /// <summary>
        /// Add a push transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPushTransition<T1, T2, T3>(this CyclopsState state, CyclopsState target, ref Action<T1, T2, T3> trigger)
        {
            Action<T1, T2, T3> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __, T3 ___)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPushTransition(target, () => fired);
        }

        /// <summary>
        /// Add a push transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPushTransition<T1, T2, T3, T4>(this CyclopsState state, CyclopsState target, ref Action<T1, T2, T3, T4> trigger)
        {
            Action<T1, T2, T3, T4> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __, T3 ___, T4 ____)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPushTransition(target, () => fired);
        }

        // =========== Delegate-Based Pop Transitions ===========

        /// <summary>
        /// Add a pop transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPopTransition(this CyclopsState state, ref Action trigger)
        {
            Action localTrigger = null;
            bool fired = false;

            void Handler()
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPopTransition(() => fired);
        }

        /// <summary>
        /// Add a pop transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPopTransition<T>(this CyclopsState state, ref Action<T> trigger)
        {
            Action<T> localTrigger = null;
            bool fired = false;

            void Handler(T _)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPopTransition(() => fired);
        }

        /// <summary>
        /// Add a pop transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPopTransition<T1, T2>(this CyclopsState state, ref Action<T1, T2> trigger)
        {
            Action<T1, T2> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPopTransition(() => fired);
        }

        /// <summary>
        /// Add a pop transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPopTransition<T1, T2, T3>(this CyclopsState state, ref Action<T1, T2, T3> trigger)
        {
            Action<T1, T2, T3> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __, T3 ___)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPopTransition(() => fired);
        }

        /// <summary>
        /// Add a pop transition that fires when the delegate is invoked.
        /// </summary>
        public static CyclopsState AddPopTransition<T1, T2, T3, T4>(this CyclopsState state, ref Action<T1, T2, T3, T4> trigger)
        {
            Action<T1, T2, T3, T4> localTrigger = null;
            bool fired = false;

            void Handler(T1 _, T2 __, T3 ___, T4 ____)
            {
                fired = true;
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state.AddPopTransition(() => fired);
        }

        // =========== Exit On Action ===========

        /// <summary>
        /// Stop the state when the delegate is invoked.
        /// </summary>
        public static CyclopsState ExitOnAction(this CyclopsState state, ref Action trigger)
        {
            Action localTrigger = null;

            void Handler()
            {
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
                state.Stop();
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state;
        }

        /// <summary>
        /// Stop the state when the delegate is invoked.
        /// </summary>
        public static CyclopsState ExitOnAction<T>(this CyclopsState state, ref Action<T> trigger)
        {
            Action<T> localTrigger = null;

            void Handler(T _)
            {
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
                state.Stop();
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state;
        }

        /// <summary>
        /// Stop the state when the delegate is invoked.
        /// </summary>
        public static CyclopsState ExitOnAction<T1, T2>(this CyclopsState state, ref Action<T1, T2> trigger)
        {
            Action<T1, T2> localTrigger = null;

            void Handler(T1 _, T2 __)
            {
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
                state.Stop();
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state;
        }

        /// <summary>
        /// Stop the state when the delegate is invoked.
        /// </summary>
        public static CyclopsState ExitOnAction<T1, T2, T3>(this CyclopsState state, ref Action<T1, T2, T3> trigger)
        {
            Action<T1, T2, T3> localTrigger = null;

            void Handler(T1 _, T2 __, T3 ___)
            {
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
                state.Stop();
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state;
        }

        /// <summary>
        /// Stop the state when the delegate is invoked.
        /// </summary>
        public static CyclopsState ExitOnAction<T1, T2, T3, T4>(this CyclopsState state, ref Action<T1, T2, T3, T4> trigger)
        {
            Action<T1, T2, T3, T4> localTrigger = null;

            void Handler(T1 _, T2 __, T3 ___, T4 ____)
            {
                // ReSharper disable once AccessToModifiedClosure
                localTrigger -= Handler;
                state.Stop();
            }

            trigger += Handler;
            localTrigger = trigger;
            Assert.IsNotNull(localTrigger, "Multicast delegate must not be null.");

            return state;
        }
    }
}

