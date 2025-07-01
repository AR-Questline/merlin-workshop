using Unity.Services.Core.Editor;

namespace Unity.Services.UserReporting.Editor
{
    /// <summary>
    /// Implementation of the <see cref="IEditorGameServiceIdentifier"/> for the User Reporting package
    /// </summary>
    public struct UserReportingIdentifier : IEditorGameServiceIdentifier
    {
        /// <summary>
        /// Gets the key for the User Reporting package
        /// </summary>
        /// <returns>The key for the service</returns>
        public string GetKey() => "UserReporting";
    }
}
