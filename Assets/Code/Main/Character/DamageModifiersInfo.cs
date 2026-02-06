namespace Awaken.TG.Main.Fights {
    public struct DamageModifiersInfo {
        public DamageModifiersInfo(bool isCritical, float criticalMultiplier, bool isSneak, float sneakMultiplier,
            bool isWeakSpot, float weakSpotMultiplier, bool isBackStab, float backStabMultiplier,
            bool isFinisher = false) {
            IsCritical = isCritical;
            CriticalMultiplier = criticalMultiplier;
            IsSneak = isSneak;
            SneakMultiplier = sneakMultiplier;
            IsWeakSpot = isWeakSpot;
            WeakSpotMultiplier = weakSpotMultiplier;
            IsBackStab = isBackStab;
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
