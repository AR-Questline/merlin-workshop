using Awaken.TG.MVC;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Feedbacks {
    public abstract class VCFeedback : ViewComponent {
        protected const string GeneralGroupName = "General";
        protected const string SpecificGroupName = "Specific";

        [SerializeField, BoxGroup(GeneralGroupName)] protected AutoPlayAt autoPlayAt = AutoPlayAt.OnStart;
        [SerializeField, BoxGroup(GeneralGroupName)] protected bool overrideAtPlay;

        Tween _tween;
        
        protected abstract Tween InternalPlay();
        protected virtual void PrePlaySetup() { }
        protected virtual void InternalStop() { }
        
        void OnEnable() {
            if (autoPlayAt == AutoPlayAt.OnEnable) {
                Play();
            }
        }

        void Start() {
            if (autoPlayAt == AutoPlayAt.OnStart) {
                Play();
            }
        }

        protected override void OnAttach() {
            if (autoPlayAt == AutoPlayAt.OnAttach) {
                Play();
            }
        }

        public void Play() {
            if (overrideAtPlay) {
                PrePlaySetup();
            }
            
            _tween = InternalPlay();
        }
        
        public void Stop() {
            InternalStop();
            UITweens.DiscardTween(ref _tween);
        }
        
        void OnDisable() {
            Stop();
        }

        protected override void OnDiscard() {
            Stop();
        }
    }

    public abstract class VCSingleFeedback : VCFeedback {
        const string LoopGroupName = "Loops";

        [SerializeField, BoxGroup(VCFeedback.GeneralGroupName)] protected float duration = 0.2f;
        [SerializeField, BoxGroup(VCFeedback.GeneralGroupName)] protected float delay;
        [SerializeField, BoxGroup(VCFeedback.GeneralGroupName)] protected Ease ease = Ease.InCubic;

        [SerializeField, BoxGroup(LoopGroupName), Tooltip("if -1, it will loop forever")] 
        protected int loops;
        [SerializeField, BoxGroup(LoopGroupName), HideIf(nameof(loops), 0)]
        protected LoopType loopType;
    }

    public enum AutoPlayAt : byte {
        [UnityEngine.Scripting.Preserve] None,
        OnStart,
        OnEnable,
        OnAttach
    }
}