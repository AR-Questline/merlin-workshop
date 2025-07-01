using System;
using System.Linq;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.Utility.Collections;

namespace Awaken.TG.Editor.BalanceTool.Data {
    public class NpcEntry {
        public string name;
        public float avgMeleeWeaponMult;
        public float avgRangedWeaponMult;
        public float avgMagicWeaponMult;
        public float avgWeaponStaminaCost;
        public NpcTemplate template;
        public bool isIncorrect;

        public ObservableTemplateValue<int> hp;
        public ObservableTemplateValue<int> stamina;
        public ObservableTemplateValue<int> armor;
        public ObservableTemplateValue<float> meleeDamage;
        public ObservableTemplateValue<float> rangedDamage;
        public ObservableTemplateValue<float> magicDamage;
        public ObservableTemplateValue<float> poiseThreshold;
        public ObservableTemplateValue<float> forceStumbleThreshold;
        public ObservableTemplateValue<int> heroKnockBack;

        public event Action ReadyToSave = delegate { };
        
        public NpcEntry(NpcTemplate template) {
            this.template = template;
            name = PrettierName(template.name);
            AvgDamage(out avgMeleeWeaponMult, out avgRangedWeaponMult, out avgMagicWeaponMult);
            hp = new ObservableTemplateValue<int>(() => template.MaxHealth, value => ApplyValue(template.OverrideHP, value));
            stamina = new ObservableTemplateValue<int>(() => template.MaxStamina, value => ApplyValue(template.OverrideStamina, value));
            armor = new ObservableTemplateValue<int>(() => template.Armor, value => ApplyValue(template.OverrideArmor, value));
            meleeDamage = new ObservableTemplateValue<float>(() => template.meleeDamage, value => ApplyValue(template.OverrideMeleeDamage, value));
            rangedDamage = new ObservableTemplateValue<float>(() => template.rangedDamage, value => ApplyValue(template.OverrideRangedDamage, value));
            magicDamage = new ObservableTemplateValue<float>(() => template.magicDamage, value => ApplyValue(template.OverrideMagicDamage, value));
            poiseThreshold = new ObservableTemplateValue<float>(() => template.poiseThreshold, value => ApplyValue(template.OverridePoiseThreshold, value));
            forceStumbleThreshold = new ObservableTemplateValue<float>(() => template.ForceStumbleThreshold, value => ApplyValue(template.OverrideForceStumbleThreshold, value));
            heroKnockBack = new ObservableTemplateValue<int>(() => template.heroKnockBack, value => ApplyValue(template.OverrideHeroKnockBck, value));
        }
        
        public void ApplyAll() {
            hp.Apply();
            stamina.Apply();
            armor.Apply();
            meleeDamage.Apply();
            rangedDamage.Apply();
            magicDamage.Apply();
            poiseThreshold.Apply();
            forceStumbleThreshold.Apply();
            heroKnockBack.Apply();
        }
            
        string PrettierName(string name) {
            return name.Replace("Faction", string.Empty).Replace("NPCTemplate", string.Empty).Replace("_", " ");
        }

        public float OutputMeleeDamage() => meleeDamage.CurrentValue * avgMeleeWeaponMult;
        public float OutputRangedDamage() => rangedDamage.CurrentValue * avgRangedWeaponMult;
        public float OutputMagicDamage() => magicDamage.CurrentValue * avgMagicWeaponMult;
        public int EffectiveHealth() => (int)(hp.CurrentValue / (1 - Damage.GetArmorDamageReduction(armor.CurrentValue * template.ArmorMultiplier)));
        public float GetAvgArmor() => Damage.GetArmorDamageReduction(armor.CurrentValue * template.ArmorMultiplier);
        public float GetStaminaPerHit() => avgWeaponStaminaCost / stamina.CurrentValue;
        public int GetHitsToKillNpc(float playerDmg) => (int)Math.Ceiling(EffectiveHealth() / playerDmg);
        public float GetStaminaPercentage(float playerDmg, float avgPlayerStamina, float playerStamina) => GetHitsToKillNpc(playerDmg) * avgPlayerStamina / playerStamina;
        public int GetMeleeHitsToKillPlayer(float playerHP) => (int)Math.Ceiling(playerHP / OutputMeleeDamage());
        public int GetRangedHitsToKillPlayer(float playerHP) => (int)Math.Ceiling(playerHP / OutputRangedDamage());
        public int GetMagicHitsToKillPlayer(float playerHP) => (int)Math.Ceiling(playerHP / OutputMagicDamage());
        public float GetHitsToBreakPoise(float poiseDmg) => poiseThreshold.CurrentValue / poiseDmg;
        public float GetHitsToForceStumble(float forceDmg) => forceStumbleThreshold.CurrentValue / forceDmg;

        void AvgDamage(out float melee, out float ranged, out float magic) {
            melee = ranged = magic = 0;
            if (template == null && template.IsAbstract) {
                return;
            }
                
            var items = template.TryGetInventoryItemsWithoutException();

            if (items != null) {
                var weapons = items.Where(item => item is { ItemTemplate: { IsAbstract: false, IsWeapon: true } }).ToList();
                
                if (weapons.Count <= 0) {
                    isIncorrect = true;
                    return;
                }

                melee = weapons.Where(i => i.ItemTemplate.IsMelee).AverageSafe(item => item.ItemTemplate.GetAttachment<ItemStatsAttachment>()?.npcDamageMultiplier ?? 1);
                ranged = weapons.Where(i => i.ItemTemplate.IsRanged).AverageSafe(item => item.ItemTemplate.GetAttachment<ItemStatsAttachment>()?.npcDamageMultiplier ?? 1);
                magic = weapons.Where(i => i.ItemTemplate.IsMagic).AverageSafe(item => item.ItemTemplate.GetAttachment<ItemStatsAttachment>()?.npcDamageMultiplier ?? 1);
                if (float.IsNaN(melee)) melee = 0;
                if (float.IsNaN(ranged)) ranged = 0;
                if (float.IsNaN(magic)) magic = 0;
                avgWeaponStaminaCost = weapons.Average(item => BalanceToolCalculator.ComputeAvgWeaponStaminaCost(item.ItemTemplate.GetAttachment<ItemStatsAttachment>()).value);
                return;
            }

            return;
        }
        
        void ApplyValue<T>(Action<T> setter, T value) {
            setter.Invoke(value);
            ReadyToSave.Invoke();
        }
    }
}
