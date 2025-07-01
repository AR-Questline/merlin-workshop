using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI {
    [RequireComponent(typeof(CanvasScaler))]
    public class VCAspectRatioScaler : ViewComponent {
        [SerializeField] CanvasScaler targetCanvasScaler;

        static readonly Vector2 ReferenceResolution = new (1920f, 1080f);
        
        const float SuperWideResolutionWidthOrHeight = 1.0f; // 32:9
        const float UltraWideResolutionWidthOrHeight = 0.85f; // 21:9, 64:27
        const float WideResolutionWidthOrHeight = 0.75f; // 17:9, 
        const float ReferenceResolutionWidthOrHeight = 0.5f; // 16:9, 16:10
        const float NarrowResolutionWidthOrHeight = 0.25f; // 4:3, 5:4, 3:2
        
        const float SuperWideThreshold = 3.0f;
        const float UltraWideThreshold = 2.0f;
        const float WideThreshold = 1.8f;
        const float ReferenceThreshold = 1.7f;
        const float NarrowThreshold = 1.1f;

        float _currentAspectRatio;
        
        // before MVC initialization
        void Awake() {
            _currentAspectRatio = ReferenceResolution.x / ReferenceResolution.y;
            targetCanvasScaler = targetCanvasScaler ? targetCanvasScaler : GetComponent<CanvasScaler>();
            SetupCanvasScaler();
            Refresh();
        }

        // after attach to MVC
        protected override void OnAttach() {
            var screenResolution = World.Any<ScreenResolution>();
            if (screenResolution != null) {
                screenResolution.ListenTo(Setting.Events.SettingRefresh, Refresh, this);
            } else {
                Log.Critical?.Error($"{nameof(ScreenResolution)} setting is missing in the world. {nameof(VCAspectRatioScaler)} may not function correctly and could cause UI issues when the resolution changes.");
            }
            
            Refresh();
        }

        void SetupCanvasScaler() {
            targetCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            targetCanvasScaler.referenceResolution = ReferenceResolution;
        }

        void Refresh() {
            if (PlatformUtils.IsEditor) {
                var mainCamera = World.Any<GameCamera>()?.MainCamera;
                if (mainCamera != null) {
                    _currentAspectRatio = (float)mainCamera.pixelWidth / mainCamera.pixelHeight;
                }
            } else {
                _currentAspectRatio = (float)Screen.width / Screen.height;
            }

            targetCanvasScaler.matchWidthOrHeight = _currentAspectRatio switch {
                > SuperWideThreshold => SuperWideResolutionWidthOrHeight,
                > UltraWideThreshold => UltraWideResolutionWidthOrHeight,
                > WideThreshold => WideResolutionWidthOrHeight,
                > ReferenceThreshold => ReferenceResolutionWidthOrHeight,
                > NarrowThreshold => NarrowResolutionWidthOrHeight,
                _ => 0f
            };
        }
        
#if UNITY_EDITOR
        [Button]
        public static void DebugRefresh() {
            var result = FindObjectsByType<VCAspectRatioScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var aspectRatioScaler in result) {
                aspectRatioScaler.Refresh();
            }
        }
#endif
    }
}
