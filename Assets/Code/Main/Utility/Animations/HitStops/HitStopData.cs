using System;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.HitStops {
    [Serializable]
    public class HitStopData {
        [SerializeField] AnimationCurve hitStopCurve;
        [SerializeField] float delayBeforeEnterHitStop = 0.05f;
        [SerializeField] float hitStopDuration = 0.35f;
        [SerializeField] float hitStopBlendToIdleDuration = 1f;
        [SerializeField, RichEnumExtends(typeof(CameraShakeType))] RichEnumReference cameraShakeType = CameraShakeType.LiteAllDirection;

        public AnimationCurve HitStopCurve => hitStopCurve;
        public float DelayBeforeEnterHitStop => delayBeforeEnterHitStop;
        public float HitStopDuration => hitStopDuration;
        public float HitStopBlendToIdleDuration => hitStopBlendToIdleDuration;
        public CameraShakeType CameraShakeType => cameraShakeType.EnumAs<CameraShakeType>();
    }
}