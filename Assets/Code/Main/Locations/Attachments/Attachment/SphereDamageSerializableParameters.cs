using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [Serializable]
    public struct SphereDamageSerializableParameters {
        [PrefabAssetReference]
        public ShareableARAssetReference vfx;
        public float vfxDuration;
        [BoxGroup("DMG", false)] public float damageRadius;
        [BoxGroup("DMG")] public float damageDuration;
        [BoxGroup("DMG")] public float damageAmount;
        [BoxGroup("DMG")] public float ngPlusDamageGain;
        [BoxGroup("DMG"), Indent] public float poiseDamage;
        [BoxGroup("DMG"), Indent] public float forceDamage;
        [BoxGroup("DMG"), Indent] public float ragdollForce;
        public LayerMask hitMask;
        public DamageType damageType;
        public DamageSubType damageSubType;
        [BoxGroup("Bools", false)] public bool inevitable;
        [BoxGroup("Bools")] public bool isPrimary;
        [BoxGroup("Bools")] public bool isDamageOverTime;
        [BoxGroup("Bools")] public bool ignoreArmor;
        [BoxGroup("Bools")] public bool canBeCritical;
        [BoxGroup("Bools")] public bool isCritical;
    }

    public static class SphereDamageSerializableParametersUtils {
        public static void ApplySphereDamage(this SphereDamageSerializableParameters shout, Vector3 origin) {
            var rawDamageData = new RawDamageData(shout.damageAmount + shout.ngPlusDamageGain * Hero.Current.NewGamePlusLevel);
            
            var baseDamageParameters = DamageParameters.Default;
            baseDamageParameters.DamageTypeData = new RuntimeDamageTypeData(shout.damageType, shout.damageSubType);
            baseDamageParameters.PoiseDamage = shout.poiseDamage;
            baseDamageParameters.ForceDamage = shout.forceDamage;
            baseDamageParameters.RagdollForce = shout.ragdollForce;
            baseDamageParameters.Inevitable = shout.inevitable;
            baseDamageParameters.IsPrimary = shout.isPrimary;
            baseDamageParameters.IsDamageOverTime = shout.isDamageOverTime;
            baseDamageParameters.IgnoreArmor = shout.ignoreArmor;
            baseDamageParameters.CanBeCritical = shout.canBeCritical;
            baseDamageParameters.Critical = shout.isCritical;
            
            var sphereDamageParameters = new SphereDamageParameters {
                rawDamageData = rawDamageData,
                duration = shout.damageDuration,
                endRadius = shout.damageRadius,
                hitMask = shout.hitMask,
                defaultDelay = 0f,
                item = null,
                baseDamageParameters = baseDamageParameters
            };
            
            // Create damage source and deal damage
            var damageSource = World.Add(new DealDamageInAreaPoint(origin));
            damageSource.AddElement(new DealDamageInSphereOverTime(sphereDamageParameters, origin))
                .ListenTo(Model.Events.BeforeDiscarded, damageSource.Discard);
        }
    }
}
