using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.BalanceTool.Data;
using Awaken.TG.Editor.BalanceTool.UI.Controls;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.UIToolkit.Utils;
using Awaken.Utility.Editor.UTK.Utils;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.BalanceTool.UI.Factory {
    public class BalanceToolTableFactory {
        public const string ColumnStat = "stat";
        public const string ColumnBaseValue = "base-value";
        public const string ColumnModifiers = "modifiers";
        public const string ColumnAddPerLevel = "add-per-level";
        public const string ColumnEffective = "effective";
        public const string ColumnNotes = "notes";
        
        public const string ColumnName = "name";
        public const string ColumnTemplate = "template";
        public const string ColumnHP = "hp";
        public const string ColumnStamina = "stamina";
        public const string ColumnStaminaPerHit = "stamina-per-hit";
        public const string ColumnArmor = "armor";
        public const string ColumnMP = "mp";
        public const string ColumAvgArmor = "avg-arm";
        public const string ColumEffectiveHP = "effective-hp";
        public const string ColumAvgMeleeMult = "avg-melee-mult";
        public const string ColumAvgRangedMult = "avg-ranged-mult";
        public const string ColumAvgMagicMult = "avg-magic-mult";
        public const string ColumMeleeDamage = "melee-damage";
        public const string ColumRangedDamage = "ranged-damage";
        public const string ColumMagicDamage = "magic-damage";
        public const string ColumOutputMeleeDmg = "output-melee-dmg";
        public const string ColumOutputRangedDmg = "output-ranged-dmg";
        public const string ColumOutputMagicDmg = "output-magic-dmg";
        public const string ColumnWeaponStaminaPerHit = "avg-stamina-per-hit";
        public const string ColumHitsToKillEnemy = "hits-to-kill-enemy";
        public const string ColumLightStaminaToKillEnemy = "light-stamina-to-kill";
        public const string ColumHeavyStaminaToKillEnemy = "heavy-stamina-to-kill";
        public const string ColumMeleeHitsToKillPlayer = "melee-hits-to-kill-player";
        public const string ColumRangedHitsToKillPlayer = "ranged-hits-to-kill-player";
        public const string ColumMagicHitsToKillPlayer = "magic-hits-to-kill-player";
        public const string ColumnPoiseThreshold = "poise-threshold";
        public const string ColumnHitsToBreakPoise = "hits-to-break-poise";
        public const string ColumnForceStumbleThreshold = "force-stumble-threshold";
        public const string ColumnHitsToForceStumble = "hits-to-force-stumble";
        public const string ColumnHeroKnockBack = "hero-knock-back";

        RPGBalanceTool _balanceTool;
        
        public void Set(RPGBalanceTool balanceTool) {
            _balanceTool = balanceTool;
        }

        public void SetupStatsList(MultiColumnListView listView, Dictionary<string, StatEntry> entries) {
            listView.itemsSource = entries.Values.ToList();
            
            listView.SetupColumn<Label>(ColumnStat, BindCell(entries, ColumnStat), headerTooltip: "Name of the stat entry");
            listView.SetupColumn<FloatField>(ColumnBaseValue, BindCell(entries, ColumnBaseValue, nameof(StatEntry.BaseValue)), headerTooltip: $"Base stat value considering start value from game data (e.g. player {HeroRPGStatType.Strength.DisplayName})");
            listView.SetupColumn(ColumnModifiers, MakeInputFloatField(), BindCell(entries, ColumnModifiers, nameof(StatEntry.Modifiers)), headerTooltip: "Modifiers are added to the base value to calculate the effective value");
            listView.SetupColumn<FloatField>(ColumnEffective, BindCell(entries, ColumnEffective, nameof(StatEntry.effective)), headerTooltip: "Effective value calculated individually for each stat, see tooltip formula for each stat field");
        }
        
        public void SetupAdditionalStatsList(MultiColumnListView listView, Dictionary<string, StatEntry> entries) {
            SetupStatsList(listView, entries);
            listView.SetupColumn(ColumnAddPerLevel, MakeInputFloatField(), BindCell(entries, ColumnAddPerLevel, nameof(StatEntry.AddPerLevel)), headerTooltip: "Multiplier per level of the hero's stats. See field tooltip for more information");
        }
        
        public void SetupOutputNpcList(MultiColumnListView listView) {
            listView.SetupColumn<Label>(ColumnName, BindCell((element, entry) => SetupNameField(element, entry.name, entry.template.name)), headerTooltip: "Name of the NPC entry");
            listView.SetupColumn<ObjectField>(ColumnTemplate, BindCell(SetupTemplateObjectField), headerTooltip: "Pick to select a template in Project window");
            listView.SetupColumn(ColumnHP, MakeEditableTemplateIntField(), BindCell((element, entry) => SetupTemplateField(element, entry.hp)), UnbindCell<int>(), headerTooltip: "Editable Health Points of the NPC");
            listView.SetupColumn(ColumMeleeDamage, MakeEditableTemplateFloatField(), BindCell((element, entry) => SetupTemplateField(element, entry.meleeDamage)), UnbindCell<float>(), headerTooltip: "Editable Npc Damage which is used to calculate the output damage");
            listView.SetupColumn(ColumRangedDamage, MakeEditableTemplateFloatField(), BindCell((element, entry) => SetupTemplateField(element, entry.rangedDamage)), UnbindCell<float>(), headerTooltip: "Editable Npc Damage which is used to calculate the output damage");
            listView.SetupColumn(ColumMagicDamage, MakeEditableTemplateFloatField(), BindCell((element, entry) => SetupTemplateField(element, entry.magicDamage)), UnbindCell<float>(), headerTooltip: "Editable Npc Damage which is used to calculate the output damage");
            listView.SetupColumn(ColumnArmor, MakeEditableTemplateIntField(), BindCell((element, entry) => SetupTemplateField(element, entry.armor)), UnbindCell<int>(), headerTooltip: "Editable Armor Points included in the calculation of the effective health");
            listView.SetupColumn(ColumnStamina, MakeEditableTemplateIntField(), BindCell((element, entry) => SetupTemplateField(element, entry.stamina)), UnbindCell<int>());
            listView.SetupColumn(ColumnPoiseThreshold, MakeEditableTemplateFloatField(), BindCell((element, entry) => SetupTemplateField(element, entry.poiseThreshold)), UnbindCell<float>(), headerTooltip: "Poise threshold value");
            listView.SetupColumn(ColumnForceStumbleThreshold, MakeEditableTemplateFloatField(), BindCell((element, entry) => SetupTemplateField(element, entry.forceStumbleThreshold)), UnbindCell<float>(), headerTooltip: "Force stumble threshold value");
            listView.SetupColumn<FloatField>(ColumAvgArmor, BindCell((element, entry) => SetupField(element, entry.GetAvgArmor() * 100, true)), headerTooltip: "Average Armor damage reduction in percentage");
            listView.SetupColumn<IntegerField>(ColumEffectiveHP, BindCell((element, entry) => SetupField(element, entry.EffectiveHealth(), true)), headerTooltip: "Effective Health Points calculated based on the Armor and HP values");
            listView.SetupColumn<FloatField>(ColumAvgMeleeMult, BindCell((element, entry) => SetupField(element, entry.avgMeleeWeaponMult, true)), headerTooltip: "Average Damage from weapons of the NPC");
            listView.SetupColumn<FloatField>(ColumAvgRangedMult, BindCell((element, entry) => SetupField(element, entry.avgRangedWeaponMult, true)), headerTooltip: "Average Damage from weapons of the NPC");
            listView.SetupColumn<FloatField>(ColumAvgMagicMult, BindCell((element, entry) => SetupField(element, entry.avgMagicWeaponMult, true)), headerTooltip: "Average Damage from weapons of the NPC");
            listView.SetupColumn<FloatField>(ColumOutputMeleeDmg, BindCell((element, entry) => SetupField(element, entry.OutputMeleeDamage(), true)), headerTooltip: "Output Damage calculated based on the Strength Linear and Average Damage");
            listView.SetupColumn<FloatField>(ColumOutputRangedDmg, BindCell((element, entry) => SetupField(element, entry.OutputRangedDamage(), true)), headerTooltip: "Output Damage calculated based on the Strength Linear and Average Damage");
            listView.SetupColumn<FloatField>(ColumOutputMagicDmg, BindCell((element, entry) => SetupField(element, entry.OutputMagicDamage(), true)), headerTooltip: "Output Damage calculated based on the Strength Linear and Average Damage");
            listView.SetupColumn<FloatField>(ColumnWeaponStaminaPerHit, BindCell((element, entry) => SetupField(element, entry.avgWeaponStaminaCost, true)), headerTooltip: "Average Stamina cost per hit based on the weapon stamina cost and NPC Stamina");
            listView.SetupColumn<FloatField>(ColumnStaminaPerHit, BindCell((element, entry) => SetupField(element, entry.GetStaminaPerHit() * 100, true)), headerTooltip: "Stamina cost per hit based on the weapon stamina cost and NPC Stamina");
            listView.SetupColumn<IntegerField>(ColumHitsToKillEnemy, BindCell((element, entry) => SetupField(element, entry.GetHitsToKillNpc(_balanceTool.avgDmg), true)), headerTooltip: "Number of hits required to kill the NPC based on the Player Damage output");
            listView.SetupColumn<FloatField>(ColumLightStaminaToKillEnemy, BindCell((element, entry) => SetupField(element, entry.GetStaminaPercentage(_balanceTool.avgDmg, _balanceTool.ComputeLightStaminaCost(), _balanceTool.ComputeEffectiveStamina()), true)), headerTooltip: "Light attack stamina cost percentage required to kill the NPC based on the Player Damage output");
            listView.SetupColumn<FloatField>(ColumHeavyStaminaToKillEnemy, BindCell((element, entry) => SetupField(element, entry.GetStaminaPercentage(_balanceTool.avgDmg, _balanceTool.ComputeHeavyStaminaCost(), _balanceTool.ComputeEffectiveStamina()), true)), headerTooltip: "Heavy attack stamina cost percentage required to kill the NPC based on the Player Damage output");
            listView.SetupColumn<IntegerField>(ColumMeleeHitsToKillPlayer, BindCell((element, entry) => SetupField(element, entry.GetMeleeHitsToKillPlayer(_balanceTool.effectiveHP), true)), headerTooltip: "Number of hits required to kill the Player based on the NPC Damage output and Player Effective Health");
            listView.SetupColumn<IntegerField>(ColumRangedHitsToKillPlayer, BindCell((element, entry) => SetupField(element, entry.GetRangedHitsToKillPlayer(_balanceTool.effectiveHP), true)), headerTooltip: "Number of hits required to kill the Player based on the NPC Damage output and Player Effective Health");
            listView.SetupColumn<IntegerField>(ColumMagicHitsToKillPlayer, BindCell((element, entry) => SetupField(element, entry.GetMagicHitsToKillPlayer(_balanceTool.effectiveHP), true)), headerTooltip: "Number of hits required to kill the Player based on the NPC Damage output and Player Effective Health");
            listView.SetupColumn<FloatField>(ColumnHitsToBreakPoise, BindCell((element, entry) => SetupField(element, entry.GetHitsToBreakPoise(_balanceTool.poiseDmg), true)), headerTooltip: "Number of hits required to break the Poise of the NPC based on the Player Damage output");
            listView.SetupColumn<FloatField>(ColumnHitsToForceStumble, BindCell((element, entry) => SetupField(element, entry.GetHitsToForceStumble(_balanceTool.forceDmg), true)), headerTooltip: "Number of hits required to force stumble the NPC based on the Player Damage output");
            listView.SetupColumn(ColumnHeroKnockBack, MakeEditableTemplateIntField(), BindCell((element, entry) => SetupTemplateField(element, entry.heroKnockBack)), UnbindCell<int>(), headerTooltip: "Hero KnockBack amount");
        }
        
        public (Func<List<NpcEntry>> ascendingSort, Func<List<NpcEntry>> descendingSort) GetNpcSorting(string columnName) {
            return columnName switch {
                ColumnName => (() => _balanceTool.npcs.OrderBy(entry => entry.name).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.name).ToList()),
                ColumnHP => (() => _balanceTool.npcs.OrderBy(entry => entry.hp.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.hp.CurrentValue).ToList()),
                ColumnStamina => (() => _balanceTool.npcs.OrderBy(entry => entry.stamina.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.stamina.CurrentValue).ToList()),
                ColumnArmor => (() => _balanceTool.npcs.OrderBy(entry => entry.armor.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.armor.CurrentValue).ToList()),
                ColumnWeaponStaminaPerHit => (() => _balanceTool.npcs.OrderBy(entry => entry.avgWeaponStaminaCost).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.avgWeaponStaminaCost).ToList()),
                ColumAvgArmor => (() => _balanceTool.npcs.OrderBy(entry => entry.GetAvgArmor()).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetAvgArmor()).ToList()),
                ColumEffectiveHP => (() => _balanceTool.npcs.OrderBy(entry => entry.EffectiveHealth()).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.EffectiveHealth()).ToList()),
                ColumAvgMeleeMult => (() => _balanceTool.npcs.OrderBy(entry => entry.avgMeleeWeaponMult).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.avgMeleeWeaponMult).ToList()),
                ColumAvgRangedMult => (() => _balanceTool.npcs.OrderBy(entry => entry.avgRangedWeaponMult).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.avgRangedWeaponMult).ToList()),
                ColumAvgMagicMult => (() => _balanceTool.npcs.OrderBy(entry => entry.avgMagicWeaponMult).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.avgMagicWeaponMult).ToList()),
                ColumMeleeDamage => (() => _balanceTool.npcs.OrderBy(entry => entry.meleeDamage.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.meleeDamage.CurrentValue).ToList()),
                ColumRangedDamage => (() => _balanceTool.npcs.OrderBy(entry => entry.rangedDamage.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.rangedDamage.CurrentValue).ToList()),
                ColumMagicDamage => (() => _balanceTool.npcs.OrderBy(entry => entry.magicDamage.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.magicDamage.CurrentValue).ToList()),
                ColumnStaminaPerHit => (() => _balanceTool.npcs.OrderBy(entry => entry.GetStaminaPerHit()).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetStaminaPerHit()).ToList()),
                ColumOutputMeleeDmg => (() => _balanceTool.npcs.OrderBy(entry => entry.OutputMeleeDamage()).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.OutputMeleeDamage()).ToList()),
                ColumOutputRangedDmg => (() => _balanceTool.npcs.OrderBy(entry => entry.OutputRangedDamage()).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.OutputRangedDamage()).ToList()),
                ColumOutputMagicDmg => (() => _balanceTool.npcs.OrderBy(entry => entry.OutputMagicDamage()).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.OutputMagicDamage()).ToList()),
                ColumHitsToKillEnemy => (() => _balanceTool.npcs.OrderBy(entry => entry.GetHitsToKillNpc(_balanceTool.avgDmg)).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetHitsToKillNpc(_balanceTool.avgDmg)).ToList()), 
                ColumLightStaminaToKillEnemy => (() => _balanceTool.npcs.OrderBy(entry => entry.GetStaminaPercentage(_balanceTool.avgDmg, _balanceTool.ComputeLightStaminaCost(), _balanceTool.ComputeEffectiveStamina())).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetStaminaPercentage(_balanceTool.avgDmg, _balanceTool.ComputeLightStaminaCost(), _balanceTool.ComputeEffectiveStamina())).ToList()),
                ColumHeavyStaminaToKillEnemy => (() => _balanceTool.npcs.OrderBy(entry => entry.GetStaminaPercentage(_balanceTool.avgDmg, _balanceTool.ComputeHeavyStaminaCost(), _balanceTool.ComputeEffectiveStamina())).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetStaminaPercentage(_balanceTool.avgDmg, _balanceTool.ComputeHeavyStaminaCost(), _balanceTool.ComputeEffectiveStamina())).ToList()),
                ColumMeleeHitsToKillPlayer => (() => _balanceTool.npcs.OrderBy(entry => entry.GetMeleeHitsToKillPlayer(_balanceTool.effectiveHP)).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetMeleeHitsToKillPlayer(_balanceTool.effectiveHP)).ToList()),
                ColumRangedHitsToKillPlayer => (() => _balanceTool.npcs.OrderBy(entry => entry.GetRangedHitsToKillPlayer(_balanceTool.effectiveHP)).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetRangedHitsToKillPlayer(_balanceTool.effectiveHP)).ToList()),
                ColumMagicHitsToKillPlayer => (() => _balanceTool.npcs.OrderBy(entry => entry.GetMagicHitsToKillPlayer(_balanceTool.effectiveHP)).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetMagicHitsToKillPlayer(_balanceTool.effectiveHP)).ToList()),
                ColumnPoiseThreshold => (() => _balanceTool.npcs.OrderBy(entry => entry.poiseThreshold.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.poiseThreshold.CurrentValue).ToList()),
                ColumnForceStumbleThreshold => (() => _balanceTool.npcs.OrderBy(entry => entry.forceStumbleThreshold.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.forceStumbleThreshold.CurrentValue).ToList()),
                ColumnHitsToBreakPoise => (() => _balanceTool.npcs.OrderBy(entry => entry.GetHitsToBreakPoise(_balanceTool.poiseDmg)).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetHitsToBreakPoise(_balanceTool.poiseDmg)).ToList()),
                ColumnHitsToForceStumble => (() => _balanceTool.npcs.OrderBy(entry => entry.GetHitsToForceStumble(_balanceTool.forceDmg)).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.GetHitsToForceStumble(_balanceTool.forceDmg)).ToList()),
                ColumnHeroKnockBack => (() => _balanceTool.npcs.OrderBy(entry => entry.heroKnockBack.CurrentValue).ToList(), () => _balanceTool.npcs.OrderByDescending(entry => entry.heroKnockBack.CurrentValue).ToList()),
                _ => (() => _balanceTool.npcs, () => _balanceTool.npcs)
            };
        }
        
        Action<VisualElement, int> UnbindCell<T>() => (element, _) => {
            if (element.dataSource is NpcEntry entry) {
                entry.ReadyToSave -= _balanceTool.NotifyTemplateChange;
                if (element is TemplateStatField<T> statField) {
                    statField.Discard();
                }
            }
        };
        
        Action<VisualElement, int> BindCell(Action<VisualElement, NpcEntry> setupCallback) => (element, i) => {
            NpcEntry entry = _balanceTool.npcs[i];
            entry.ReadyToSave += _balanceTool.NotifyTemplateChange;
            element.dataSource = entry;
            setupCallback(element, entry);
        };
        
        Action<VisualElement, int> BindCell(Dictionary<string, StatEntry> entries, string columnName) => (element, i) => {
            StatEntry entry = entries.ElementAt(i).Value;
            element.dataSource = entry;
            SetupListElement(columnName, element, entry);
        };
        
        Action<VisualElement, int> BindCell(Dictionary<string, StatEntry> entries, string columnName, string propertyName) => (element, i) => {
            BindCell(entries, columnName)(element, i);
            
            var binding = DataBindingUtils.CreateDefaultBinding(propertyName, element);
            element.SetBinding(binding.target, binding.dataBinding);
        };
        
        static Func<VisualElement> MakeInputFloatField() => () => new FloatField { label = "#" };
        static Func<VisualElement> MakeEditableTemplateFloatField() => () => new TemplateStatField<float>(new FloatField());
        static Func<VisualElement> MakeEditableTemplateIntField() => () => new TemplateStatField<int>(new IntegerField());

        void SetupListElement(string columnName, VisualElement element, StatEntry entry) {
            switch (columnName) {
                case ColumnStat:
                    SetupNameField(element, entry.name, entry.description);
                    break;
                case ColumnBaseValue:
                    SetupField(element, entry.BaseValue, true);
                    break;
                case ColumnModifiers:
                    SetupField(element, entry.Modifiers);
                    break;
                case ColumnAddPerLevel:
                    SetupField(element, entry.AddPerLevel, true);
                    if (string.IsNullOrEmpty(entry.addPerLevelStat)) break;
                    element.tooltip = $"Dependent on the <b>{entry.addPerLevelStat}</b> stat";
                    break;
                case ColumnEffective:
                    SetupField(element, entry.effective, true);
                    element.tooltip = entry.effectiveFormula;
                    ((FloatField) element).RegisterValueChangedCallback(_ => _balanceTool.UpdateComputedStats());
                    break;
                case ColumnNotes:
                    TextField notesField = (TextField) element;
                    notesField.value = entry.notes;
                    break;
                default:
                    throw MultiColumnViewUtils.UnknownColumn(columnName);
            }
        }

        void SetupNameField(VisualElement element, string name, string tooltip) {
            Label nameLabel = (Label) element;
            nameLabel.text = name;
            nameLabel.tooltip = tooltip;
        }
        
        void SetupTemplateObjectField(VisualElement element, NpcEntry entry) {
            ObjectField templateField = (ObjectField) element;
            templateField.value = entry.template;
        }
        
        static void SetupField<T>(VisualElement element, T value, bool isReadOnly = false) {
            TextValueField<T> field = (TextValueField<T>) element;
            field.value = value;
            
            if (isReadOnly) {
                field.isReadOnly = true;
                field.SetEnabled(false);
            }
        }
        
        void SetupTemplateField<T>(VisualElement element, ObservableTemplateValue<T> value) where T : struct {
            if (element is TemplateStatField<T> field) {
                field.valueField[0].AddToClassList("editable-field");
                SetupField(field.valueField, value.CurrentValue);
                
                field.RegisterChangeCallback(evt => {
                    value.OnValueChanged += _ => _balanceTool.NotifyTemplateStatChange();
                    value.CurrentValue = evt.newValue;
                    field.SetApplyControlsState(value.IsChanged, value.TemplateValue.Invoke());
                    _balanceTool.UpdateNpcsList();
                });
                        
                field.RegisterApplyCallback(() => {
                    value.Apply();
                    field.SetApplyControlsState(false, value.TemplateValue.Invoke());
                });
                        
                field.SetApplyControlsState(value.IsChanged, value.TemplateValue.Invoke());
            }
        } 
    }
}
