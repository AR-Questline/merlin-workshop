using System;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.SolarBeam {
    [Serializable]
    public struct SolarBeamData {
        public DamageParametersData damageData;
        public RawDamageData rawDamageData;
        public float maxRange;
        public Vector3 raycastOffset;
        public RaycastCheck targetDetection;
        public bool pierceTargets;
    }

    [Serializable]
    public struct SolarBeamCreationData {
        public DamageParametersData damageData;
        public float maxRange;
        public Vector3 raycastOffset;
        public RaycastCheck targetDetection;
        public bool pierceTargets;

        public SolarBeamData Create(RawDamageData damage) {
            return new SolarBeamData() {
                damageData = this.damageData,
                rawDamageData = damage,
                maxRange = this.maxRange,
                raycastOffset = this.raycastOffset,
                targetDetection = this.targetDetection,
                pierceTargets = this.pierceTargets
            };
        }
        
        public static SolarBeamCreationData Default => new () {
            damageData = DamageParametersData.DefaultSolarBeam
        };
    }
}
