using System.Collections.Generic;
using Unity.Services.Core.Editor;
using UnityEditor;
using UnityEditor.CrashReporting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.UserReporting.Editor
{
    class UserReportingSettingsProvider : EditorGameServiceSettingsProvider
    {
        static readonly string[] k_SupportedPlatforms = new string[] { "Android", "iOS", "Linux", "Mac", "PC", "WebGL",
                                                                       "Windows 8 Universal", "Windows 10 Universal" };

        public UserReportingSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) {}

        protected override IEditorGameService EditorGameService => EditorGameServiceRegistry.Instance.GetEditorGameService<UserReportingIdentifier>();
        protected override string Title => "User Reporting";
        protected override string Description => "Discover and collect feedback from users.";

        protected override VisualElement GenerateServiceDetailUI()
        {
            var containerAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath.Common);
            VisualElement containerUI = null;
            if (containerAsset != null)
            {
                containerUI = containerAsset.CloneTree().contentContainer;
                SetupStyleSheets(containerUI);

                AddUserReportingUI(containerUI);
                SetupLearnMoreButton(containerUI);
            }

            return containerUI;
        }

        void SetupStyleSheets(VisualElement parentElement)
        {
            parentElement.AddToClassList("userreporting");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath.Common);
            if (styleSheet != null)
            {
                parentElement.styleSheets.Add(styleSheet);
            }
        }

        void SetupGoToDashboard(VisualElement parentVisualElement)
        {
            var crashServiceGoToDashboard = parentVisualElement.Q(UxmlNode.UserReportGoToDashboard);
            if (crashServiceGoToDashboard != null)
            {
                var clickable = new Clickable(() =>
                {
                    EditorGameService.OpenDashboard();
                });
                crashServiceGoToDashboard.AddManipulator(clickable);
            }
        }

        static void AddUserReportingUI(VisualElement parentElement)
        {
            var userReportContainer = parentElement.Q(className: UssClassName.UserReportContainer);
            if (userReportContainer != null)
            {
                userReportContainer.Clear();
                userReportContainer.Add(PlatformSupportUiHelper.GeneratePlatformSupport(k_SupportedPlatforms));
            }
        }

        static void SetupLearnMoreButton(VisualElement parentElement)
        {
            var learnMoreButton = parentElement.Q(UxmlNode.LearnMore);
            if (learnMoreButton != null)
            {
                var clickable = new Clickable(() =>
                {
                    EditorGameServiceAnalyticsSender.SendProjectSettingsLearnMoreEvent();
                    Application.OpenURL(URL.LearnMore);
                });
                learnMoreButton.AddManipulator(clickable);
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
#if ENABLE_EDITOR_GAME_SERVICES
            return new UserReportingSettingsProvider(GenerateProjectSettingsPath("User Reporting"), SettingsScope.Project);
#else
            return null;
#endif
        }

        static class URL
        {
            public const string LearnMore = "https://docs.unity.com/cloud-diagnostics/UserReporting/AboutUserReporting.html";
        }

        static class UxmlPath
        {
            public const string Common = "Packages/com.unity.services.user-reporting/Editor/UXML/UserReportingProjectSettings.uxml";
        }

        static class UxmlNode
        {
            public const string UserReportGoToDashboard = "GoToDashboard";
            public const string LearnMore = "LearnMore";
        }

        static class UssClassName
        {
            public const string UserReportContainer = "cloud-diag-user-report";
        }

        static class UssPath
        {
            public const string Common = "Packages/com.unity.services.user-reporting/Editor/USS/UserReportStyleSheet.uss";
        }
    }
}
