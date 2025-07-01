using Unity.Services.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.UserReporting.Editor
{
    class UserReportingService : IEditorGameService
    {
        public string Name => "User Reporting";
        public IEditorGameServiceIdentifier Identifier { get; } = new UserReportingIdentifier();
        public bool RequiresCoppaCompliance => false;
        public bool HasDashboard => true;
        public string GetFormattedDashboardUrl()
        {
#if ENABLE_EDITOR_GAME_SERVICES
            return $"https://developer.cloud.unity3d.com/diagnostics/orgs/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/user-reports";
#else
            return string.Empty;
#endif
        }

        public IEditorGameServiceEnabler Enabler { get; } = null;
    }
}
