using Awaken.TG.Main.Fights.DamageInfo;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public static class FallDamageUtil {
        const float BaseMultiplier = 0.58f, BasePower = 2.73f;
        
        public static void DealFallDamage(this ICharacter character, float damageToDeal) {
            if (damageToDeal < 1) {
                return;
            }

            DamageParameters param = new() {
                Position = character.Coords,
                PoiseDamage = 0,
                ForceDamage = 0,
                Radius = 0,
                Direction = Vector3.zero,
                DamageTypeData = new RuntimeDamageTypeData(DamageType.Fall),
                CanBeCritical = false,
                Critical = false,
                IgnoreArmor = true,
                Inevitable = true,
                IsPrimary = false,
            };
            
            Damage damage = new(param, character, character, new RawDamageData(damageToDeal));
            character.HealthElement.TakeDamage(damage);
        }
        
        public static float GetFallDamage(float heightDifference, float multiplier = 1f) => 
            Mathf.Pow(heightDifference * BaseMultiplier, BasePower) * multiplier;
    }
}