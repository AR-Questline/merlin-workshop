using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class OnCameraRotationPart : BasePart {
        public float dragAmount = 500f;
        float _draggedValue;

        public override async UniTask<bool> OnRun(TutorialContext context) {
            _draggedValue = 0f;
            //var listener = context.world.Any<FightCamera>()?.ListenTo(FightCamera.Events.AfterDragged, OnDragged, context.vc);

            while (_draggedValue < dragAmount && !context.IsDone) {
                await UniTask.NextFrame();
            }

            //context.world.EventSystem.RemoveListener(listener);
            return _draggedValue >= dragAmount;
        }
        
        void OnDragged(float drag) {
            _draggedValue += Mathf.Abs(drag) * 5f;
        }
    }
}