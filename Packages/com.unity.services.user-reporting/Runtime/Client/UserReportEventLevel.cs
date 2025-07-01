using System;

namespace Unity.Services.UserReporting.Client
{
    /// <summary>
    /// Enum representing the level at which an event will be logged.
    /// </summary>
    public enum UserReportEventLevel
    {
        /// <summary>
        /// Default informative logging, like what would be printed to the Unity Console.
        /// </summary>
        Info = 0,
        /// <summary>
        /// Logging level for an event caused by a successful operation.
        /// </summary>
        Success = 1,
        /// <summary>
        /// Logging level for an event which would be considered a warning, such as a known risk.
        /// </summary>
        Warning = 2,
        /// <summary>
        /// Logging level for an event which would be considered an error, such as a critical operation failure.
        /// </summary>
        Error = 3
    }
}
