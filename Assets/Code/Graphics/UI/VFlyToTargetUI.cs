using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Graphics.UI {
    [UsesPrefab("UI/VFlyToTargetUI")]
    public class VFlyToTargetUI : View<FlyToTargetUI> {
        
        public Image image;
        public AnimationCurve xCurve;
        public AnimationCurve yCurve;
        public AnimationCurve offsetCurve;
        public AnimationCurve sizeCurve;
        float _timer = 0;

        RectTransform _rectTransform;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            image.sprite = Target.Data.Sprite;
            transform.position = Target.Data.StartPosition;
            _rectTransform = GetComponent<RectTransform>();
            _rectTransform.sizeDelta = Target.Data.StartSize;
        }

        void Update() {
            if (Target.Data.Target == null) {
                Target.Discard();
                return;
            }
            
            _timer += Time.deltaTime;
            var sampler = _timer / Target.Data.Time;
            var targetPos = Target.Data.Target.position + Vector3.LerpUnclamped(Target.Data.Offset * Target.Data.OffsetMagnitude, Vector3.zero, offsetCurve.Evaluate(sampler));
            var xPos = Mathf.Lerp( Target.Data.StartPosition.x, targetPos.x, xCurve.Evaluate(sampler));
            var yPos = Mathf.Lerp( Target.Data.StartPosition.y, targetPos.y, yCurve.Evaluate(sampler));
            var zPos = Mathf.Lerp( Target.Data.StartPosition.z, targetPos.z, sampler);
            _rectTransform.sizeDelta = Vector2.Lerp(Target.Data.StartSize, Target.Data.EndSize, sizeCurve.Evaluate(sampler));
            transform.position = new Vector3(xPos, yPos, zPos);

            if (_timer > Target.Data.Time) {
                Target.Discard();
            }
        }
    }
}