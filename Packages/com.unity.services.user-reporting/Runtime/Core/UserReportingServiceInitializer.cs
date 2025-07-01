using Unity.Services.Core.Configuration.Internal;
using UnityEngine;
using Unity.Services.Core.Device.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Scheduler.Internal;
using Unity.Services.UserReporting.Internal;
using Task = System.Threading.Tasks.Task;

namespace Unity.Services.UserReporting
{
    class UserReportingInitializer : IInitializablePackage
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Register()
        {
            CoreRegistry.Instance.RegisterPackage(new UserReportingInitializer())
                .DependsOn<IActionScheduler>()
                .DependsOn<IInstallationId>()
                .DependsOn<IProjectConfiguration>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            var actionScheduler = registry.GetServiceComponent<IActionScheduler>();
            var installationId = registry.GetServiceComponent<IInstallationId>();
            var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();

            var service = new UserReportingServiceInternal(
                projectConfiguration.GetString(UserReportingInternalUtilities.PackageName, ""),
                installationId.GetOrCreateIdentifier(), actionScheduler);

            UserReportingService.serviceInternalUserReportingServiceInstance = service;

            // Use default configuration.
            UserReportingService.Instance.Configure();

            return Task.CompletedTask;
        }
    }
}
