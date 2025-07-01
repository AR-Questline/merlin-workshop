using Unity.Services.UserReporting.Internal;
using Unity.Services.Core;

namespace Unity.Services.UserReporting
{
    /// <summary>
    /// The entry class to the Cloud Diagnostics service.
    /// </summary>
    public static class UserReportingService
    {
        internal static UserReportingServiceInternal serviceInternalUserReportingServiceInstance;

        /// <summary>
        /// The default singleton instance to access the User Reporting service.
        /// </summary>
        /// <exception cref="ServicesInitializationException">
        /// This exception is thrown if the <code>UnityServices.InitializeAsync()</code>
        /// has not finished before accessing the singleton.
        /// </exception>
        public static IUserReporting Instance
        {
            get
            {
                if (serviceInternalUserReportingServiceInstance == null)
                {
                    throw new ServicesInitializationException("Singleton is not initialized. " +
                        "Please call UnityServices.InitializeAsync() to initialize.");
                }
                return serviceInternalUserReportingServiceInstance;
            }
        }
    }
}
