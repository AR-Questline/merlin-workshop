using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using Awaken.Utility.Cameras;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.UI {
    [UsesPrefab("UI/" + nameof(VBlurBackground))]
    public class VBlurBackground : View<BlurBackground> {
        [SerializeField] RawImage blurBackground;
        [SerializeField] GameObject content;
        [SerializeField] GameObject consoleBackground;
        [SerializeField] GameObject blurVolume;
        [SerializeField] GameObject opaqueObject;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        Texture2D _tmpTexture;
        RectTransform _rectTransform;
        Transform _contentTarget;
        
        protected override void OnInitialize() {
            _rectTransform = (RectTransform)transform;
            content.SetActiveOptimized(false);
            opaqueObject.SetActiveOptimized(Target.config.isOpaque);
            SetupBackgroundForPlatform();
        }
        
        void SetupBackgroundForPlatform() {
            bool onlyBackground = BlurBackground.WithoutBlur;
            blurVolume.TrySetActiveOptimized(onlyBackground && Target.config.useBlurVolume);
            consoleBackground.TrySetActiveOptimized(onlyBackground);
            blurBackground.TrySetActiveOptimized(!onlyBackground);
        }

        public async UniTaskVoid CreateBackgroundWithDelay(int delayFrames = 1, CancellationToken cancellationToken = default) {
            if (await AsyncUtil.DelayFrame(this, delayFrames, cancellationToken)) {
                CreateBackground();
            }
        }
        
        public void CreateBackground() {
            _contentTarget = Target.CurrentTargetContent;
            _rectTransform.SetParent(_contentTarget.parent);
            
            if (BlurBackground.WithoutBlur) {
                ShowBackground();
            } else {
                PrepareBlur().Forget();
            }
        }
        
        async UniTaskVoid PrepareBlur() {
            // wait to ensure the overlay UI is fully rendered 
            if (await AsyncUtil.WaitForEndOfFrame(this, Target.BlurCancellationTokenSource) == false) return;
            
            _tmpTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            _tmpTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            _tmpTexture.Apply();
            
            blurBackground.texture = _tmpTexture;
            ShowBackground();
        }

        void ShowBackground() {
            var siblingIndex = _contentTarget.GetSiblingIndex();
            _rectTransform.SetSiblingIndex(siblingIndex < 0 ? 0 : siblingIndex);
            
            _rectTransform.StretchToParent();
            _rectTransform.localScale = Vector3.one;
            content.SetActiveOptimized(true);
        }
        
        void ReleaseResources() {
            if(_tmpTexture == null) return;
            Object.Destroy(_tmpTexture);
            blurBackground.texture = null;
        }

        protected override IBackgroundTask OnDiscard() {
            ReleaseResources();
            return base.OnDiscard();
        }
        
#if UNITY_EDITOR
        [Button]
        public static void DebugConsole() {
            var result = FindObjectsByType<VBlurBackground>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var blurBackground in result) {
                blurBackground.SetupBackgroundForPlatform();
                blurBackground.CreateBackgroundWithDelay().Forget();
            }
        }
#endif
    }
}