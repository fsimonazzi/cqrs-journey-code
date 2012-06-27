// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Infrastructure.Instrumentation
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    public class InfrastructureInstrumentation : IDisposable
    {
        public const string CqrsThreadPoolCategoryName = "CQRS - Thread Pool";
        public const string CurrentMinWorkerThreadsCounterName = "Min worker threads";
        public const string CurrentMaxWorkerThreadsCounterName = "Max worker threads";
        public const string CurrentAvailableWorkerThreadsCounterName = "Available worker threads";
        public const string CurrentMinCompletionThreadsCounterName = "Min completion threads";
        public const string CurrentMaxCompletionThreadsCounterName = "Max completion threads";
        public const string CurrentAvailableCompletionThreadsCounterName = "Available completion threads";

        private readonly bool instrumentationEnabled;

        private readonly PerformanceCounter minWorkerThreadsCounter;
        private readonly PerformanceCounter maxWorkerThreadsCounter;
        private readonly PerformanceCounter availableWorkerThreadsCounter;
        private readonly PerformanceCounter minCompletionThreadsCounter;
        private readonly PerformanceCounter maxCompletionThreadsCounter;
        private readonly PerformanceCounter availableCompletionThreadsCounter;

        public InfrastructureInstrumentation(bool instrumentationEnabled, string instanceName)
        {
            this.instrumentationEnabled = instrumentationEnabled;

            if (this.instrumentationEnabled)
            {
                this.minWorkerThreadsCounter = new PerformanceCounter(CqrsThreadPoolCategoryName, CurrentMinWorkerThreadsCounterName, instanceName, false);
                this.maxWorkerThreadsCounter = new PerformanceCounter(CqrsThreadPoolCategoryName, CurrentMaxWorkerThreadsCounterName, instanceName, false);
                this.availableWorkerThreadsCounter = new PerformanceCounter(CqrsThreadPoolCategoryName, CurrentAvailableWorkerThreadsCounterName, instanceName, false);
                this.minCompletionThreadsCounter = new PerformanceCounter(CqrsThreadPoolCategoryName, CurrentMinCompletionThreadsCounterName, instanceName, false);
                this.maxCompletionThreadsCounter = new PerformanceCounter(CqrsThreadPoolCategoryName, CurrentMaxCompletionThreadsCounterName, instanceName, false);
                this.availableCompletionThreadsCounter = new PerformanceCounter(CqrsThreadPoolCategoryName, CurrentAvailableCompletionThreadsCounterName, instanceName, false);
            }
        }

        public void UpdateThreadPoolCounters()
        {
            if (this.instrumentationEnabled)
            {
                try
                {
                    int minWorkerThreads, maxWorkerThreads, availableWorkerThreads;
                    int minCompletionThreads, maxCompletionThreads, availableCompletionThreads;

                    ThreadPool.GetMinThreads(out minWorkerThreads, out minCompletionThreads);
                    ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionThreads);
                    ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionThreads);

                    this.minWorkerThreadsCounter.RawValue = minWorkerThreads;
                    this.maxWorkerThreadsCounter.RawValue = maxWorkerThreads;
                    this.availableWorkerThreadsCounter.RawValue = availableWorkerThreads;
                    this.minCompletionThreadsCounter.RawValue = minCompletionThreads;
                    this.maxCompletionThreadsCounter.RawValue = maxCompletionThreads;
                    this.availableCompletionThreadsCounter.RawValue = availableCompletionThreads;
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.instrumentationEnabled)
                {
                    this.minWorkerThreadsCounter.Dispose();
                    this.maxWorkerThreadsCounter.Dispose();
                    this.availableWorkerThreadsCounter.Dispose();
                    this.minCompletionThreadsCounter.Dispose();
                    this.maxCompletionThreadsCounter.Dispose();
                    this.availableCompletionThreadsCounter.Dispose();
                }
            }
        }
    }
}
