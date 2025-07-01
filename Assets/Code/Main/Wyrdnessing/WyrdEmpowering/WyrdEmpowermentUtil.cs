using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;

namespace Awaken.TG.Main.Wyrdnessing.WyrdEmpowering {
    public static class WyrdEmpowermentUtil {
        public static void CheckEmpowermentNeed(NpcElement npcElement, bool wyrdConverted) {
            if (!npcElement.CanBeWyrdEmpowered) return;
            
            bool shouldBeEmpowered = wyrdConverted;
            bool stateChanged =  npcElement.HasElement<EmpowermentStatTweak>() != shouldBeEmpowered;

            if (stateChanged) {
                if (shouldBeEmpowered) {
                    EnableEmpowerment(npcElement);
                } else {
                    DisableEmpowerment(npcElement);
                }
            }
        }

        static void EnableEmpowerment(NpcElement npcElement) {
            foreach (var wyrdEmpoweredStat in GameConstants.Get.WyrdEmpoweredStats) {
                bool untweakable = GetOldValues(npcElement, wyrdEmpoweredStat.StatEffected, out float oldMaxValue, out float oldValue);

                Stat tweakedStat = npcElement.ParentModel.Stat(wyrdEmpoweredStat.StatEffected);
                if (tweakedStat == null) continue;
                
                EmpowermentStatTweak statTweak = new(tweakedStat, wyrdEmpoweredStat.GetStrength(), wyrdEmpoweredStat.EffectType, npcElement);

                if (untweakable) {
                    AdjustProportionallyUntweakableStats(npcElement, statTweak.StatType, oldValue, oldMaxValue);
                }
            }
        }

        static void DisableEmpowerment(NpcElement npcElement) {
            foreach (var statTweak in npcElement.Elements<EmpowermentStatTweak>().Reverse()) {
                bool untweakable = GetOldValues(npcElement, statTweak.StatType, out float oldMaxValue, out float oldValue);

                statTweak.Discard();

                if (untweakable) {
                    AdjustProportionallyUntweakableStats(npcElement, statTweak.StatType, oldValue, oldMaxValue);
                }
            }
        }

        static bool GetOldValues(NpcElement npcElement, StatType statType, out float oldMaxValue, out float oldValue) {
            oldMaxValue = 1;
            oldValue = 1;
            if (statType == AliveStatType.MaxHealth) {
                oldValue = npcElement.ParentModel.Stat(AliveStatType.Health);
                oldMaxValue = npcElement.ParentModel.Stat(AliveStatType.MaxHealth);
            } else if (statType == CharacterStatType.MaxMana) {
                oldValue = npcElement.ParentModel.Stat(CharacterStatType.Mana);
                oldMaxValue = npcElement.ParentModel.Stat(CharacterStatType.MaxMana);
            } else if (statType == CharacterStatType.MaxStamina) {
                oldValue = npcElement.ParentModel.Stat(CharacterStatType.Stamina);
                oldMaxValue = npcElement.ParentModel.Stat(CharacterStatType.MaxStamina);
            } else {
                return false;
            }
            return true;
        }
        
        static void AdjustProportionallyUntweakableStats(NpcElement npcElement, StatType statType, float oldValue, float oldMaxValue) {
            if (statType == AliveStatType.MaxHealth) {
                AdjustStatProportionally(npcElement.ParentModel.Stat(AliveStatType.MaxHealth), oldValue, oldMaxValue, npcElement.Health);
            } else if (statType == CharacterStatType.MaxMana) {
                AdjustStatProportionally(npcElement.ParentModel.Stat(CharacterStatType.MaxMana), oldValue, oldMaxValue, npcElement.CharacterStats.Mana);
            } else if (statType == CharacterStatType.MaxStamina) {
                AdjustStatProportionally(npcElement.ParentModel.Stat(CharacterStatType.MaxStamina), oldValue, oldMaxValue, npcElement.CharacterStats.Stamina);
            }

            return;

            void AdjustStatProportionally(float newMax, float oldValue, float oldMax, Stat targetStat) {
                float ratio = oldValue / oldMax;
                float newValue = newMax * ratio;
                targetStat.SetTo(newValue);
            }
        }
    }
}