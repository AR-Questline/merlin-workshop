using System;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;

namespace Awaken.TG.Main.AI.Graphs {
    public readonly struct TargetRange {
        readonly RangeBetween _between;
        readonly float _range;

        public TargetRange(RangeBetween between, float range) {
            _between = between;
            _range = range;
        }

        public float GetRange(RangeBetween between, NpcElement npc) {
            if (_between == between) {
                return _range;
            }
            
            float myRadius = npc.Radius;
            float targetRadius = npc.GetCurrentTarget()?.Radius ?? 0F;

            float borderRange = _between switch {
                RangeBetween.CenterCenter => _range - myRadius - targetRadius,
                RangeBetween.BorderBorder => _range,
                RangeBetween.BorderCenter => _range - targetRadius,
                RangeBetween.CenterBorder => _range - myRadius,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            return between switch {
                RangeBetween.CenterCenter => borderRange + myRadius + targetRadius,
                RangeBetween.BorderBorder => borderRange,
                RangeBetween.BorderCenter => borderRange + targetRadius,
                RangeBetween.CenterBorder => borderRange + myRadius,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    public enum RangeBetween {
        BorderBorder,
        CenterCenter,
        BorderCenter,
        CenterBorder,
    }
}