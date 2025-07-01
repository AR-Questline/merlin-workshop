namespace Awaken.TG.Main.Fights {
    public struct DamageModifiersInfo {
        public DamageModifiersInfo(float criticalMultiplier, float sneakMultiplier, float weakSpotMultiplier, float backStabMultiplier, bool isFinisher = false) {
            IsCritical = criticalMultiplier > 0;
            CriticalMultiplier = criticalMultiplier;
            IsSneak = sneakMultiplier > 0;
            SneakMultiplier = sneakMultiplier;
            IsWeakSpot = weakSpotMultiplier > 0;
            WeakSpotMultiplier = weakSpotMultiplier;
            IsBackStab = backStabMultiplier > 0;
            BackStabMultiplier = backStabMultiplier;
            IsFinisher = isFinisher;
        }
        
        DamageModifiersInfo(DamageModifiersInfo info, bool isFinisher) {
            IsCritical = info.IsCritical;
            CriticalMultiplier = info.CriticalMultiplier;
            IsSneak = info.IsSneak;
            SneakMultiplier = info.SneakMultiplier;
            IsWeakSpot = info.IsWeakSpot;
            WeakSpotMultiplier = info.WeakSpotMultiplier;
            IsBackStab = info.IsBackStab;
            BackStabMultiplier = info.BackStabMultiplier;
            IsFinisher = isFinisher;
        }

        [UnityEngine.Scripting.Preserve]
        public DamageModifiersInfo MarkAsFinisher() {
            return new DamageModifiersInfo(this, true);
        }

        public bool IsFinisher { get; }
        public bool IsCritical { get; }
        public float CriticalMultiplier { get; }
        public bool IsSneak { get; }
        public float SneakMultiplier { get; }
        public bool IsWeakSpot { get; }
        public float WeakSpotMultiplier { get; }
        public bool IsBackStab { get; }
        public float BackStabMultiplier { get; }
        public bool AnyCritical => IsCritical || IsSneak || IsWeakSpot || IsBackStab;
    }
}
