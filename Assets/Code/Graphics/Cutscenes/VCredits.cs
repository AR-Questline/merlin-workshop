using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    [UsesPrefab("UI/Credits/VCredits")]
    public class VCredits : View<Credits>, IPromptHost {
        [SerializeField] RectTransform contentParent;
        [SerializeField] float scrollSpeed = 50f;
        [SerializeField] RectTransform creditsSize;
        [SerializeField] ARFmodEventEmitter musicEmitter;
        [SerializeField] ARFmodEventEmitter snapshotEmitter;
        [SerializeField] Transform promptHost;

        bool _started;
        float _endY;
        float _time;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public Transform PromptsHost => promptHost;

        protected override void OnMount() {
            PlayAudio();
            StartCredits().Forget();
        }

        async UniTaskVoid StartCredits() {
            await AsyncUtil.WaitForEndOfFrame(this);
            contentParent.anchoredPosition = Vector3.zero;
            // remember to add empty space at the bottom of the credits in the prefab
            _endY = contentParent.rect.height;
            _started = true;
            _time = Time.unscaledTime;
        }

        void Update() {
            if (!_started) {
                return;
            }
            
            Vector2 pos = contentParent.anchoredPosition;
            pos.y = scrollSpeed * (Time.unscaledTime - _time);

            if (pos.y >= _endY) {
                _started = false;
                Target.Discard();
                return;
            }

            contentParent.anchoredPosition = pos;
        }

        protected override IBackgroundTask OnDiscard() {
            StopAudio();
            return base.OnDiscard();
        }
        
        void PlayAudio() {
            // musicEmitter.Play();
            // snapshotEmitter.Play();
        }

        public void StopAudio() {
            // musicEmitter.Stop();
            // snapshotEmitter.Stop();
        }
    }
}