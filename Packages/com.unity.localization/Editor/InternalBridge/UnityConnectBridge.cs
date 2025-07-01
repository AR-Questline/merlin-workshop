using UnityEditor.Connect;

namespace UnityEditor.Localization.Bridge
{
    internal static class UnityConnectBridge
    {
        public static string GetOrganizationId() => UnityConnect.instance.GetOrganizationId();

        public static string GetOrganizationName() => UnityConnect.instance.GetOrganizationName();

        public static string GetOrganizationForeignKey() => UnityConnect.instance.GetOrganizationForeignKey();

        public static string GetProjectGUID() => UnityConnect.instance.GetProjectGUID();

        public static string GetProjectName() => UnityConnect.instance.GetProjectName();

        public static string GetAccessToken() => UnityConnect.instance.GetAccessToken();
    }
}
