using UnityEditor;

namespace Unity.Services.UserReporting.Editor
{
    static class UserReportingTopMenu
    {
        const int k_ConfigureMenuPriority = 100;
        const string k_ServiceMenuRoot = "Services/User Reporting/";

        [MenuItem(k_ServiceMenuRoot + "Configure", priority = k_ConfigureMenuPriority)]
        static void ShowProjectSettings()
        {
            EditorGameServiceAnalyticsSender.SendTopMenuConfigureEvent();
            SettingsService.OpenProjectSettings("Project/Services/User Reporting");
        }
    }
}
