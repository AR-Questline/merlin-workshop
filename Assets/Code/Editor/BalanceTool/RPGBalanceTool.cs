using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.BalanceTool.Data;
using Awaken.TG.Editor.BalanceTool.Presets;
using Awaken.TG.Editor.BalanceTool.UI.Factory;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.UTK;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.BalanceTool {
    public class RPGBalanceTool : EditorWindowPresenter<RPGBalanceTool> {
        public List<NpcEntry> npcs = new();
        public float effectiveHP;
        public float avgDmg;
        public float poiseDmg;
        public float forceDmg;
        
        VisualElement _inputSection;
        VisualElement _outputSection;
        VisualElement _computedStatsSection;
        VisualElement _statsPresetSection;
        MultiColumnListView _npcsListView;

        ObjectField _statsPresetField;
        ObjectField _heroTemplateField;
        ToolbarToggle _abstractTemplateToggle;
        ToolbarToggle _fleeingTemplateToggle;
        ToolbarToggle _incorrectTemplateToggle;
        ToolbarButton _saveTemplateButton;
        ToolbarButton _applyAllButton;

        Label _playerLevel;
        Label _effectiveHP;
        Label _effectiveStamina;
        Label _damageOutput;
        Label _lightStaminaCost;
        Label _heavyStaminaCost;
        Label _weaponDamage;
        Label _armorValue;

        BalanceToolData _data = new();
        BalanceToolTableFactory _tableFactory = new();
        List<NpcEntry> _allNpcs = new();
        
        public RPGBalanceTool() {
            WindowName = "RPG Balance Tool";
        }

        [MenuItem("TG/Design/Balance Tool")]
        public static void ShowWindow() {
            GetWindow();
        }

        public override void CreateGUI() {
            _tableFactory.Set(this);
            
            // always call base.CreateGUI() first to properly setup the window
            base.CreateGUI();
            SetupPlayer();
            SetupEquipment();
            SetupInputSection();
            SetupOutputSection();
            SetupOutputToolbar();
            
            UpdateBaseAdditionalStats();
            UpdateComputedStats();
        }
        
        protected override void CacheVisualElements(VisualElement windowRoot) {
            _inputSection = windowRoot.Q<VisualElement>("input");
            _statsPresetSection = windowRoot.Q<VisualElement>("stats-preset-root");
            _statsPresetField = windowRoot.Q<ObjectField>("stats-preset");
            _abstractTemplateToggle = windowRoot.Q<ToolbarToggle>("abstract-visible");
            _fleeingTemplateToggle = windowRoot.Q<ToolbarToggle>("fleeing-visible");
            _incorrectTemplateToggle = windowRoot.Q<ToolbarToggle>("incorrect-visible");
            _heroTemplateField = windowRoot.Q<ObjectField>("hero-template");

            _computedStatsSection = windowRoot.Q<VisualElement>("computed");
            _playerLevel = _computedStatsSection.Q<Label>("player-level-value");
            _effectiveHP = _computedStatsSection.Q<Label>("ehp-value");
            _effectiveStamina = _computedStatsSection.Q<Label>("esp-value");
            _damageOutput = _computedStatsSection.Q<Label>("dmg-output-value");
            _lightStaminaCost = _computedStatsSection.Q<Label>("light-sp-cost");
            _heavyStaminaCost = _computedStatsSection.Q<Label>("heavy-sp-cost");
            _weaponDamage = _computedStatsSection.Q<Label>("dmg-value");
            _armorValue = _computedStatsSection.Q<Label>("armor-value");

            _data.SetEquipment(EquipmentSlotType.MainHand, _inputSection.Q<ObjectField>("main-hand"));
            _data.SetEquipment(EquipmentSlotType.Helmet, _inputSection.Q<ObjectField>("head"));
            _data.SetEquipment(EquipmentSlotType.Cuirass, _inputSection.Q<ObjectField>("body"));
            _data.SetEquipment(EquipmentSlotType.Gauntlets, _inputSection.Q<ObjectField>("arms"));
            _data.SetEquipment(EquipmentSlotType.Greaves, _inputSection.Q<ObjectField>("legs"));
            _data.SetEquipment(EquipmentSlotType.Boots, _inputSection.Q<ObjectField>("feet"));
            _data.SetEquipment(EquipmentSlotType.Back, _inputSection.Q<ObjectField>("cape"));
            
            _outputSection = windowRoot.Q<VisualElement>("output");
            _npcsListView = _outputSection.Q<MultiColumnListView>();
            _npcsListView.columnSortingChanged += () => {
                SortColumnDescription sortedColumn = _npcsListView.sortedColumns.Last();
                npcs = SortNpcs(sortedColumn.columnName, sortedColumn.direction);
                _npcsListView.RefreshItems();
            };
        }
        
        List<NpcEntry> SortNpcs(string columnName, SortDirection direction) {
            (Func<List<NpcEntry>> ascendingSort, Func<List<NpcEntry>> descendingSort) sorting = _tableFactory.GetNpcSorting(columnName);
            return direction switch {
                SortDirection.Ascending => sorting.ascendingSort(),
                SortDirection.Descending => sorting.descendingSort(),
                _ => npcs
            };
        }
        
        void SetupPlayer() {
            BalanceToolStatsPreset preset = _statsPresetField.value as BalanceToolStatsPreset;

            if (_heroTemplateField.value == null) {
                Log.Minor?.Error("No hero template selected");
                return;
            }

            _heroTemplateField.value = preset.heroTemplate.gameObject;
            
            foreach (var eqItem in _data.playerEquipment) {
                eqItem.Value.field.value = preset.GetEquipmentTemplate(eqItem.Key);
            }
            
            _data.SetPlayerTemplate(((GameObject)_heroTemplateField.value).GetComponent<HeroTemplate>());
        }

        void SetupEquipment() {
            var playerEquipmentClone = _data.playerEquipment.ToDictionary(pair => pair.Key, pair => pair.Value);
            
            foreach (var eqItem in playerEquipmentClone) {
                UpdateEquipment(eqItem.Value.field.value);
                eqItem.Value.field.RegisterValueChangedCallback(change => {
                    UpdateEquipment(change.newValue);
                    UpdateComputedStats();
                });
            }
        }
        
        void UpdateEquipment(Object eqItemObject) {
            if (eqItemObject is GameObject eqItem && eqItem.TryGetComponent<ItemTemplate>(out var template) && template.TryGetComponent<ItemStatsAttachment>(out var itemStats)) {
                var tuple = _data[template.EquipmentType.MainSlotType];
                tuple.stats = itemStats;
                _data[template.EquipmentType.MainSlotType] = tuple;
            }
        }

        void SetupInputSection() {
            BalanceToolStatsPreset preset = _statsPresetField.value as BalanceToolStatsPreset;

            if (preset == null) {
                Log.Minor?.Error("No default preset selected");
                return;
            }

            Button resetButton = _statsPresetSection.Q<Button>("reset-stats");
            resetButton.clicked += ResetStats;

            Button saveButton = _statsPresetSection.Q<Button>("save-stats");
            saveButton.clicked += () => {
                BalanceToolStatsPresetSaveWindow.ShowWindow(_data);
            };
            
            MultiColumnListView modifiersList = _inputSection.Q<MultiColumnListView>("general-modifiers");
            _data.AddEntries(preset.modifiers, StatEntryType.Modifiers);
            _tableFactory.SetupStatsList(modifiersList, _data.modifiers);
            
            MultiColumnListView baseStatsList = _inputSection.Q<MultiColumnListView>("player-base-stats");
            _data.AddEntries(preset.baseStatsEntries, StatEntryType.Base);
            _tableFactory.SetupStatsList(baseStatsList, _data.baseStatsEntries);
            
            MultiColumnListView proficiencyList = _inputSection.Q<MultiColumnListView>("proficiency-stats");
            _data.AddEntries(preset.proficiencyStatsEntries, StatEntryType.Proficiency);
            _tableFactory.SetupStatsList(proficiencyList, _data.proficiencyStatsEntries);
            
            _data.AddEntries(preset.additionalStatsEntries, StatEntryType.Additional);
            SetupAdditionalStatsList();
        }
        
        void ResetStats() {
            BalanceToolStatsPreset preset = _statsPresetField.value as BalanceToolStatsPreset;
            
            if (preset == null) {
                Log.Minor?.Error("No default preset selected");
                return;
            }
            
            _data.SetPlayerTemplate(preset.heroTemplate);
            _heroTemplateField.value = preset.heroTemplate.gameObject;

            foreach (var eqItem in _data.playerEquipment) {
                eqItem.Value.field.value = preset.GetEquipmentTemplate(eqItem.Key);
            }
            
            _data.OverrideEntries(preset.baseStatsEntries);
            _data.OverrideEntries(preset.proficiencyStatsEntries);
            _data.OverrideEntries(preset.additionalStatsEntries);
            _data.OverrideEntries(preset.modifiers);
            
            UpdateBaseAdditionalStats();
            UpdateComputedStats();
        }
        
        void UpdateBaseAdditionalStats() {
            _data[AdditionalStatEntryEnum.HP].OverrideStat(AliveStatType.MaxHealth, HeroRPGStatType.Endurance, _data.PlayerTemplate.maxHealth);
            _data[AdditionalStatEntryEnum.Stamina].OverrideStat(CharacterStatType.MaxStamina, HeroRPGStatType.Strength, _data.PlayerTemplate.MaxStamina);
            _data[AdditionalStatEntryEnum.CarryLimit].OverrideStat(HeroStatType.EncumbranceLimit, HeroRPGStatType.Endurance, _data.PlayerTemplate.encumbranceLimit);
            _data[AdditionalStatEntryEnum.CriticalChance].OverrideStat(HeroStatType.CriticalChance, HeroRPGStatType.Perception, _data.PlayerTemplate.criticalChance);
        }

        void SetupAdditionalStatsList() {
            _data[AdditionalStatEntryEnum.HP].SetEffectiveFormula(_data[HeroRPGStatType.Endurance]);
            _data[AdditionalStatEntryEnum.Stamina].SetEffectiveFormula(_data[HeroRPGStatType.Strength]);
            _data[AdditionalStatEntryEnum.CarryLimit].SetEffectiveFormula(_data[HeroRPGStatType.Endurance]);
            _data[AdditionalStatEntryEnum.CriticalChance].SetEffectiveFormula(_data[HeroRPGStatType.Perception]);
            
            MultiColumnListView listView = _inputSection.Q<MultiColumnListView>("additional-stats");
            _tableFactory.SetupAdditionalStatsList(listView, _data.additionalStatsEntries);
        }
        
        void SetupOutputToolbar() {
            Toolbar toolbar = _outputSection.Q<Toolbar>();

            ToolbarSearchField searchField = toolbar.Q<ToolbarSearchField>();
            searchField.RegisterValueChangedCallback(Filter);
            
            _saveTemplateButton = toolbar.Q<ToolbarButton>("save-templates");
            _saveTemplateButton.clicked += SaveTemplates;
            _saveTemplateButton.SetEnabled(false);
            
            _applyAllButton = toolbar.Q<ToolbarButton>("apply-all");
            _applyAllButton.clicked += ApplyAll;
            _applyAllButton.SetEnabled(false);

            ToolbarButton refreshTemplates = toolbar.Q<ToolbarButton>("refresh-npcs");
            refreshTemplates.clicked += _npcsListView.RefreshItems;
            
            _abstractTemplateToggle.RegisterValueChangedCallback(change => FillNpcsList(change.newValue, _fleeingTemplateToggle.value, _incorrectTemplateToggle.value));
            _fleeingTemplateToggle.RegisterValueChangedCallback(change => FillNpcsList(_abstractTemplateToggle.value, change.newValue, _incorrectTemplateToggle.value));
            _incorrectTemplateToggle.RegisterValueChangedCallback(change => FillNpcsList(_abstractTemplateToggle.value, _fleeingTemplateToggle.value, change.newValue));
        }
        
        public void NotifyTemplateChange() {
            _saveTemplateButton.SetEnabled(true);
        }
        
        public void NotifyTemplateStatChange() {
            _applyAllButton.SetEnabled(true);
        }
        
        void ApplyAll() {
            _allNpcs.ForEach(entry => entry.ApplyAll());
            _applyAllButton.SetEnabled(false);
            SaveTemplates();
        }
        
        void SaveTemplates() {
            AssetDatabase.SaveAssets();
            _saveTemplateButton.SetEnabled(false);
            _npcsListView.RefreshItems();
        }

        void SetupOutputSection() { 
            _allNpcs = TemplatesSearcher.FindAllOfType<NpcTemplate>().Select(template => new NpcEntry(template: template)).ToList();
            FillNpcsList(_abstractTemplateToggle.value, _fleeingTemplateToggle.value, _incorrectTemplateToggle.value);
            _tableFactory.SetupOutputNpcList(_npcsListView);
        }

        void FillNpcsList(bool hideAbstracts, bool hideFleeingNpcs, bool hideIncorrectSetupNpcs) {
            npcs = hideAbstracts ? _allNpcs.Where(entry => !entry.name.Contains("Abstract")).ToList() : _allNpcs;
            npcs = hideFleeingNpcs ? npcs.Where(entry => entry.template.CrimeReactionArchetype != CrimeReactionArchetype.FleeingPeasant).ToList() : npcs;
            npcs = hideIncorrectSetupNpcs ? npcs.Where(entry => entry.isIncorrect == false).ToList() : npcs;
            _npcsListView.itemsSource = npcs;
        }
        
        void Filter(ChangeEvent<string> filter) {
            if (string.IsNullOrEmpty(filter.newValue)) {
                SetNpcsList(_allNpcs);
                return;
            }
            
            List<NpcEntry> newRoots = new();
            _allNpcs.ForEach(root => {
                if (root.name.Contains(filter.newValue, StringComparison.OrdinalIgnoreCase)) {
                    newRoots.Add(root);
                }
            });

            SetNpcsList(newRoots);
        }
        
        void SetNpcsList(List<NpcEntry> newNpcs) {
            npcs = newNpcs;
            _npcsListView.itemsSource = npcs;
            _npcsListView.Rebuild();
        }

        public void UpdateComputedStats() {
            _data.UpdateEntries();
            
            var effectiveHPWithInfo = ComputeEffectiveHP();
            effectiveHP = effectiveHPWithInfo.value;
            var avgDmgWithInfo = ComputeAvgDmg();
            avgDmg = avgDmgWithInfo.value;
            poiseDmg = _data[EquipmentSlotType.MainHand].stats.poiseDamage;
            forceDmg = _data[EquipmentSlotType.MainHand].stats.forceDamage;
            var avgWeaponDmgWithInfo = BalanceToolCalculator.ComputeAvgWeaponDmg(_data[EquipmentSlotType.MainHand].stats);
            
            _playerLevel.text = ComputePlayerLevel().ToString();
            _effectiveHP.text = effectiveHP.ToString("F", CultureInfo.InvariantCulture);
            _effectiveHP.tooltip = effectiveHPWithInfo.formula;
            _effectiveStamina.text = ComputeEffectiveStamina().ToString(CultureInfo.InvariantCulture);
            _damageOutput.text = avgDmg.ToString("F", CultureInfo.InvariantCulture);
            _damageOutput.tooltip = avgDmgWithInfo.formula;
            _lightStaminaCost.text = $"{ComputeLightStaminaCost().ToString("F", CultureInfo.InvariantCulture)} | {(ComputeLightStaminaCost() / ComputeEffectiveStamina()).ToString("P", CultureInfo.InvariantCulture)}";
            _heavyStaminaCost.text = $"{ComputeHeavyStaminaCost().ToString("F", CultureInfo.InvariantCulture)} | {(ComputeHeavyStaminaCost() / ComputeEffectiveStamina()).ToString("P", CultureInfo.InvariantCulture)}";
            _weaponDamage.text = avgWeaponDmgWithInfo.value.ToString("F", CultureInfo.InvariantCulture);
            _weaponDamage.tooltip = avgWeaponDmgWithInfo.formula;
            _armorValue.text = ComputeEqArmor().ToString("F", CultureInfo.InvariantCulture);
            
            _npcsListView.RefreshItems();
        }
        
        public void UpdateNpcsList() {
            _npcsListView.RefreshItems();
        }

        public int ComputePlayerLevel() {
            return BalanceToolCalculator.ComputePlayerLevel(_data.baseStatsEntries.Values);
        }
        
        public (float value, string formula) ComputeEffectiveHP() {
            float hp = _data[AdditionalStatEntryEnum.HP].effective;
            float dmgReduction = Damage.GetArmorDamageReduction(ComputeEqArmor());
            return BalanceToolCalculator.ComputeEffectiveHP(hp, dmgReduction);
        }

        public float ComputeEffectiveStamina() {
            float stamina = _data[AdditionalStatEntryEnum.Stamina].effective;
            return BalanceToolCalculator.ComputeEffectiveStamina(stamina);
        } 
        
        public (float value, string formula) ComputeAvgDmg() {
            return BalanceToolCalculator.ComputeAvgDmg(_data[EquipmentSlotType.MainHand].stats, _data, _data.PlayerTemplate);
        } 
        
        public float ComputeHeavyStaminaCost() {
            return BalanceToolCalculator.ComputeHeavyStaminaCost(_data[EquipmentSlotType.MainHand].stats, _data);
        }
        
        public float ComputeLightStaminaCost() {
            return BalanceToolCalculator.ComputeLightStaminaCost(_data[EquipmentSlotType.MainHand].stats, _data);
        }
        
        public float ComputeEqArmor() {
            return BalanceToolCalculator.ComputeEqArmor(_data);
        }
    }
}
