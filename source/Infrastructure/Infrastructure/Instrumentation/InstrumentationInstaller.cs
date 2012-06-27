using System.ComponentModel;
using System.Diagnostics;

namespace Infrastructure.Instrumentation
{
    [RunInstaller(true)]
    public partial class InstrumentationInstaller : System.Configuration.Install.Installer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "By design")]
        public InstrumentationInstaller()
        {
            InitializeComponent();

            // Thread pool
            {
                var installer = new PerformanceCounterInstaller { CategoryName = InfrastructureInstrumentation.CqrsThreadPoolCategoryName, CategoryType = PerformanceCounterCategoryType.MultiInstance };
                this.Installers.Add(installer);

                installer.Counters.Add(new CounterCreationData(InfrastructureInstrumentation.CurrentMinWorkerThreadsCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
                installer.Counters.Add(new CounterCreationData(InfrastructureInstrumentation.CurrentMaxWorkerThreadsCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
                installer.Counters.Add(new CounterCreationData(InfrastructureInstrumentation.CurrentAvailableWorkerThreadsCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
                installer.Counters.Add(new CounterCreationData(InfrastructureInstrumentation.CurrentMinCompletionThreadsCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
                installer.Counters.Add(new CounterCreationData(InfrastructureInstrumentation.CurrentMaxCompletionThreadsCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
                installer.Counters.Add(new CounterCreationData(InfrastructureInstrumentation.CurrentAvailableCompletionThreadsCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
            }
        }
    }
}
