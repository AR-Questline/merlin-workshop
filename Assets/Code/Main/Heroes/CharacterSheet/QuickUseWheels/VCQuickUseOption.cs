using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.UI.RadialMenu;
using DG.Tweening;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public abstract class VCQuickUseOption : VCRadialMenuOption<QuickUseWheelUI> {
        const float SequenceDuration = 0.2f;
        const float ChosenAlpha = 0.5f;

        [SerializeField] Image chosenIndicator;
        
        Tween _hoverSequence;
        bool _hovered;
        protected EventReference _selectNegativeSound;

        protected VQuickUseWheelUI VQuickUseWheel => (VQuickUseWheelUI)RadialMenu;
        
        protected virtual void Start() {
            ChangeChosenAlpha(0f);
        }

        protected override void OnAttach() {
            base.OnAttach();
            _selectNegativeSound = CommonReferences.Get.AudioConfig.LightNegativeFeedbackSound;
        }

        public override void ResetOption() {
            _hovered = false;
            _hoverSequence.Kill();
            _hoverSequence = null;
            ChangeChosenAlpha(0f);
            base.ResetOption();
        }

        public override void OnHoverStart() {
            MoveIn();
            _hovered = true;
            OnShow();
        }
        public override void OnHoverEnd() {
            MoveOut();
            OnHide();
            _hovered = false;
        }

        void MoveIn() {
            MoveSequence(ChosenAlpha);
        }
        
        void MoveOut() {
            MoveSequence(0f);
        }

        void MoveSequence(float targetAlpha) {
            _hoverSequence?.Kill(true);
            _hoverSequence = chosenIndicator.DOFade(targetAlpha, SequenceDuration).SetUpdate(true).SetEase(Ease.OutQuad);
        }

        void ChangeChosenAlpha(float alpha) {
            Color color = chosenIndicator.color;
            color.a = alpha;
            chosenIndicator.color = color;
            
        }

        void Update() {
            if (_hovered) {
                NotifyHover();
            }
        }

        protected abstract void NotifyHover();
        protected abstract void OnShow();
        protected abstract void OnHide();
    }
}