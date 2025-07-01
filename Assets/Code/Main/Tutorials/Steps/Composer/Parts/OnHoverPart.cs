using System;
using Awaken.TG.Main.Tutorials.Steps.Composer.Helpers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class OnHoverPart : BasePart {
        public float hoverTime = 0f;
        public VCHoverable hoverable;

        bool _hovered;
        float _timer = -1f;
        
        public override async UniTask<bool> OnRun(TutorialContext context) {
            hoverable.onHover += OnHoverChange;
            
            while (_timer < hoverTime && !context.IsDone) {
                if (_hovered) {
                    _timer += Time.unscaledDeltaTime;
                }
                await UniTask.NextFrame();
            }

            hoverable.onHover -= OnHoverChange;
            return true;
        }

        void OnHoverChange(bool hovered) {
            _hovered = hovered;
            _timer = _hovered ? 0f : -1f;
        }
    }
}