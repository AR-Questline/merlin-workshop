using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.UserReporting;

namespace Awaken.TG.Main.UI.Bugs {
    [SpawnsView(typeof(VModalBlocker), false)]
    [SpawnsView(typeof(VUserBugReporting))]
    public partial class UserBugReporting : AutoBugReporting, IUIStateSource {
        // === Fields & Properties
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime();
        VUserBugReporting View => View<VUserBugReporting>();

        bool _spawnedFromSettings;

        public UserBugReporting(bool spawnedFromSettings) {
            _spawnedFromSettings = spawnedFromSettings;
        }

        protected override void OnFullyInitialized() {
            ReConfigure();
            // Hide MainView for screenshot
            HideViews();
            TakeScreenshot(1280, 720);
            ShowViewWithDelay().Forget();
        }

        void HideViews() {
            View.Hide();
            World.Any<MenuUI>()?.MainView.gameObject.SetActive(false);
        }

        async UniTaskVoid ShowViewWithDelay() {
            await UniTask.DelayFrame(2);
            View.Show();
            World.Any<MenuUI>()?.MainView.gameObject.SetActive(true);
        }

        public void Close() {
            if (_spawnedFromSettings) {
                World.Add(new AllSettingsUI());
            }
            Discard();
        }

        public async UniTaskVoid CreateUserReport(string summary, string description) {
            if (IsCreatingUserReport || UnityServices.State != ServicesInitializationState.Initialized) {
                return;
            }

            IsCreatingUserReport = true;
            View.ShowProgressPanel();
            
            await UniTask.NextFrame();
            
            CreateAttachments();
            UserReportingService.Instance.CreateNewUserReport(() => CreateReport(summary, description));
        }

        protected override void OnSendProgress(float progress) {
            if (View.ProgressText != null) {
                string progressText = $"{progress:P}";
                View.ProgressText.text = progressText;
            }
        }

        protected override void OnSendResult(bool success) {
            View.ShowResult(success);
            _isSubmitting = false;
        }
    }
}
