﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Core
{
    /// <summary>
    /// Encapsulates a thread-safe, retryable asynchronous initialization operation that is lazily invoked.
    /// The encapsulated operation will awaited until it completes. If it fails, the next time
    /// <see cref="EnsureInitialized"/> in called, the operation is restarted.
    /// </summary>
    public class RetryableInitializationOperation : IDisposable
    {
        private readonly Func<Task> _operation;
        private readonly bool _continueOnCapturedContext;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private Task _task;

        private TimeSpan _minimumRetryTime = TimeSpan.Zero;
        private TimeSpan _minimumDelayBetweenInvocations = TimeSpan.Zero;

        public RetryableInitializationOperation(Func<Task> operation)
            : this(operation, false)
        {
        }

        public RetryableInitializationOperation(Func<Task> operation, bool continueOnCapturedContext)
        {
            EnsureArg.IsNotNull(operation, nameof(operation));
            _operation = operation;
            _continueOnCapturedContext = continueOnCapturedContext;
        }
        
        public RetryableInitializationOperation(Func<Task> operation, TimeSpan minimumRetryTime, TimeSpan minimumDelayBetweenInvocations)
            : this(operation, false)
        {
            _minimumRetryTime = minimumRetryTime;
            _minimumDelayBetweenInvocations = minimumDelayBetweenInvocations;
        }

        /// <summary>
        /// Peeks to see if the value has been initialized
        /// </summary>
        public bool IsInitialized => _task?.IsCompletedSuccessfully == true;

        /// <summary>
        /// When invoked for the first time, starts the async operation
        /// and awaits its completion. If the task succeeds, subsequent invocations
        /// of this method will always return a completed task. If the task fails,
        /// the next call will restart the operation. The restart is done with
        /// synchronization so only one running task will exist at a time.
        /// </summary>
        /// <returns>A task representing the completion of the initialization operation.</returns>
        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Task reference may be updated by another thread.")]
        public async ValueTask EnsureInitialized()
        {
            if (IsInitialized)
            {
                return;
            }

            StopWatch totalTime = new Stopwatch();

            if (_task == null)
            {
                await _semaphore.WaitAsync().ConfigureAwait(_continueOnCapturedContext);

                try
                {
                    if (_task == null)
                    {
                        _task = _operation();
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            TimeSpan elapsed = TimeSpan.Zero; // set to zero to guarantee a potential call in loop
            TimeSpan lastInvocationDuration = TimeSpan.MaxValue; // set to MaxValue to prevent sleep in first call
            while(_task.IsFaulted){
                StopWatch currentInvocation = new Stopwatch();
                if(elapsed > _minimumRetryTime){
                    break;
                }
                if( lastInvocationDuration < _minimumDelayBetweenInvocations){
                    Thread.Sleep(_minimumDelayBetweenInvocations - lastInvocationDuration);
                }

                if(_task.IsFaulted) {
                    await _semaphore.WaitAsync().ConfigureAwait(_continueOnCapturedContext);

                    try
                    {
                        if (_task.IsFaulted)
                        {
                            // try again
                            _task = _operation();
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                elapsed = totalTime.Elapsed;
                lastInvocationDuration = currentInvocation.Elapsed;
            }
            await _task.ConfigureAwait(_continueOnCapturedContext);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore?.Dispose();
                _semaphore = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
