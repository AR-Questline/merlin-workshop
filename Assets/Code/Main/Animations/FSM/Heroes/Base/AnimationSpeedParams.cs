using Animancer;
using Awaken.TG.Main.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Base {
    public readonly struct AnimationSpeedParams {
        public bool IsHeavy { get; }
        public AnimancerLayer Layer { get; }
        public AnimationCurve SpeedMultiplyCurve { get; }
        public WeaponRestriction Restriction { get; }
        public AnimationSpeedParams(bool isHeavy, AnimancerLayer layer, AnimationCurve speedMultiplyCurve, WeaponRestriction restriction) {
            IsHeavy = isHeavy;
            Layer = layer;
            SpeedMultiplyCurve = speedMultiplyCurve;
            Restriction = restriction;
        }
    }
}