using System;
using Sirenix.OdinInspector;
using ProjectileType = Awaken.TG.Main.Heroes.Items.Attachments.ItemProjectileAttachment.ItemProjectileData.ProjectileType;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    [Serializable]
    public struct ProjectileLogicData {
        [NonSerialized] public ProjectileType EDITOR_ProjectileType;
        
        public float lifetime;
        public bool showLifetimeStartVFX;
        [ShowIf(nameof(showLifetimeStartVFX))] public bool lifeTimeStartVFXOnFirePointPosition;
        public bool piercing;
        [ShowIf(nameof(piercing))] public bool limitedPiercing;
        [ShowIf(nameof(limitedPiercing))] public int piercingLimit;

        [HideIf(nameof(HideDirectDamage))] public bool dealDirectDamageOnContact;
        [ShowIf(nameof(UsesContactExplosions))] public bool explodeOnContact;
        [ShowIf(nameof(UsesOtherExplosions))] public bool explodeOnEnviroHit;
        [ShowIf(nameof(UsesOtherExplosions))] public bool explodeOnLifetimeEnd;
        
        bool UsesContactExplosions => EDITOR_ProjectileType is ProjectileType.ExplodingArrow or ProjectileType.MagicProjectile;
        bool UsesOtherExplosions => EDITOR_ProjectileType is ProjectileType.MagicProjectile;
        bool HideDirectDamage => explodeOnContact || EDITOR_ProjectileType is not ProjectileType.MagicProjectile;

        public static ProjectileLogicData Default =>
            new() {
                lifetime = 5f,
                showLifetimeStartVFX = false,
                piercing = false,
                limitedPiercing = false,
                piercingLimit = 1,
                dealDirectDamageOnContact = true,
                explodeOnContact = false,
                explodeOnEnviroHit = false,
                explodeOnLifetimeEnd = false
            };
        
        public static ProjectileLogicData DefaultArrow =>
            new() {
                lifetime = 90f,
                showLifetimeStartVFX = false,
                piercing = false,
                limitedPiercing = false,
                piercingLimit = 1,
                dealDirectDamageOnContact = true,
                explodeOnContact = false,
                explodeOnEnviroHit = false,
                explodeOnLifetimeEnd = false
            };
        
        [UnityEngine.Scripting.Preserve]
        public static ProjectileLogicData DefaultThrowable =>
            new() {
                lifetime = 90f,
                showLifetimeStartVFX = false,
                piercing = false,
                limitedPiercing = false,
                piercingLimit = 1,
                dealDirectDamageOnContact = true,
                explodeOnContact = false,
                explodeOnEnviroHit = false,
                explodeOnLifetimeEnd = false
            };
    }
}