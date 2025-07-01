using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Costs;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills.Cooldowns;
using Awaken.TG.Main.Skills.Passives;
using Awaken.TG.Main.Skills.Units.Masters;
using Awaken.TG.Main.Skills.Units.Passives;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.VisualScripts.Units.Typing;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills {
    public partial class Skill : Element<ISkillOwner>, ITagged, ITextVariablesContainer, INamed {
        public override ushort TypeForSerialization => SavedModels.Skill;

        const string LearnEffects = "on learn";
        const string ForgetEffects = "on forget";
        const string EquipEffects = "on equip";
        const string UnEquipEffects = "on unequip";
        const string SubmitEffects = "on submit";
        const string PerformEffects = "on perform";
        const string CancelEffects = "on cancel";
        const string LearnRepetitiveEffects = "on learn repetitive";
        const string ForgetRepetitiveEffects = "on forget repetitive";
        const string EquipRepetitiveEffects = "on equip repetitive";
        const string UnEquipRepetitiveEffects = "on unequip repetitive";
        const string SubmitRepetitiveEffects = "on submit repetitive";
        const string CancelRepetitiveEffects = "on cancel repetitive";
        const string ChargeStepIncreaseEffects = "on charge step increase";
        const string ChargeLevelIncreaseEffects = "on charge level increase";
        
        [Saved] public SkillGraph Graph { get; private set; }
        [Saved] List<SkillVariable> _variables;
        [Saved] List<SkillRichEnum> _enums;
        [Saved] List<SkillDatum> _datums;
        
        [Saved] List<SkillAssetReference> _assetReferences;
        [Saved] List<SkillTemplate> _templates;

        [Saved] List<SkillVariable> _variableOverrides;
        [Saved] List<SkillRichEnum> _enumOverrides;
        [Saved] List<SkillDatum> _datumOverrides;
        
        [Saved(false)] bool _isLearned;
        [Saved(false)] bool _isEquipped;
        [Saved(false)] bool _isSubmitted;

        Dictionary<string, SkillComputable> _computables = new();
        ICharacter _tempOwnerOverride;

        bool _customRestore;
        TokenText _description;
        TokenText _tooltip;

        VSkillMachine _vSkillMachine;
        
        public List<ICost> ReservedCosts { get; } = new();
        
        // === Getters
        
        public ICollection<string> Tags => Graph.Tags;

        public string DisplayName => ParentModel switch {
            Status status => status.DisplayName,
            ItemEffects itemEquip => itemEquip.Item.Template.ItemName,
            Talent talent => talent.TalentName,
            _ => string.Empty
        };

        public string DebugName => "Skill of Graph: " + (Graph != null ? Graph.DebugName : "Null");
        public string Description => _description.GetValue(Owner, this);

        public string SourceDescription => ParentModel switch {
            Status status => status.Description,
            ItemEffects itemEquip => itemEquip.Item.DescriptionFor(Owner),
            Talent talent => talent.Template.GetLevel(talent.Level).Description(talent, talent.Level),
            _ => Description
        };
        public IEnumerable<Keyword> Keywords => Graph.Keywords;
        public ShareableSpriteReference Icon => ParentModel switch {
            Status status => status.Icon,
            ItemEffects itemEquip => itemEquip.Item.Template.IconReference,
            _ => Graph.Icon
        };
        public bool HiddenOnUI => ParentModel switch {
            Status status => status.HiddenOnUI,
            ItemEffects itemEquip => itemEquip.Item.HiddenOnUI,
            Talent talent => false,
            _ => true
        };

        public Item SourceItem => (ParentModel as ItemEffects)?.Item;
        
        public string DescriptionFor(ICharacter owner) => _description?.GetValue(owner, this);
        [UnityEngine.Scripting.Preserve] public string Tooltip => _tooltip.GetValue(Owner, this);

        [UnityEngine.Scripting.Preserve] public ShareableSpriteReference IconReference => Graph.Icon;

        public ICharacter Owner => _tempOwnerOverride ?? ParentModel.Character;
        bool ShouldNotEnterState => HasBeenDiscarded || Owner == null || Owner.HasBeenDiscarded;
        bool ShouldNotExitState => WasDiscarded || Owner is { WasDiscarded: true };

        public bool IsLearned => _isLearned;
        public bool IsEquipped => _isEquipped;
        public bool IsSubmitted => _isSubmitted;

        public ScriptMachineWithSkill Machine => _vSkillMachine?.Machine;
        VSkillMachine SkillMachine => _vSkillMachine ??= CreateVSkillMachine();

        // === Constructors
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] Skill() { }

        public Skill(SkillGraph graph) {
            Graph = graph;
        }

        // === Model Lifecycle

        protected override void OnAfterDeserialize() {
            _variables ??= new();
            _enums ??= new();
            _datums ??= new();
            _assetReferences ??= new();
            _templates ??= new();
        }

        protected override void OnRestore() {
            if (!_customRestore) {
                this.ListenToLimited(Model.Events.BeforeFullyInitialized, _ => {
                    Refresh();
                    
                    if (IsSubmitted) {
                        OnCancelRepetitive();
                    }
                    if (IsEquipped) {
                        OnUnequipRepetitive();
                    }
                    if (IsLearned) {
                        OnForgetRepetitive();
                    }
                    if (IsLearned) {
                        OnLearnRepetitive();
                    }
                    if (IsEquipped) {
                        OnEquipRepetitive();
                    }
                    if (IsSubmitted) {
                        OnSubmitRepetitive();
                    }
                }, this);
            }
        }

        protected override void OnFullyInitialized() {
            if (_description == null) {
                Refresh();
            }
            TriggerChange();
        }

        VSkillMachine CreateVSkillMachine() {
            var vSkillMachine = new VSkillMachine();
            World.Services.Get<VSkillMachineParent>().Initialize(this, vSkillMachine);
            return vSkillMachine;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Forget();
            World.Services.Get<VSkillMachineParent>().Discard(this, _vSkillMachine);
            _vSkillMachine = null;
        }

        public void AssignVariables(SkillReference reference) {
            _variables = reference.variables.Select(v => v.Copy()).ToList();
            _enums = reference.enums.Select(e => e.Copy()).ToList();
            _datums = reference.datums.Select(d => d.Copy()).ToList();
            _assetReferences = reference.assetReferences.Select(e => e.Copy()).ToList();
            _templates = reference.templates.Select(e => e.Copy()).ToList();
        }

        [UnityEngine.Scripting.Preserve]
        public void InitEmptyVariables() {
            _variables = new();
            _enums = new();
            _datums = new();
            _assetReferences = new();
            _templates = new();
        }

        public void MarkForCustomRestore() {
            _customRestore = true;
        }
        
        public void CustomRestore(SkillReference reference) {
            if (!_customRestore) {
                Log.Important?.Error($"Skill CustomRestore without marking for it. Skill: {this}", Graph);
            }
            
            if (IsSubmitted) {
                OnCancelRepetitive();
            }
            if (IsEquipped) {
                OnUnequipRepetitive();
            }
            if (IsLearned) {
                OnForgetRepetitive();
            }
            
            AssignVariables(reference);

            if (IsLearned) {
                OnLearnRepetitive();
            }
            if (IsEquipped) {
                OnEquipRepetitive();
            }
            if (IsSubmitted) {
                OnSubmitRepetitive();
            }

            _customRestore = false;
            TriggerChange();
        }

        // === Visual Graphs Communication

        Flow CreateFlow() => Flow.New(SkillMachine.Machine.GetReference().AsReference());
        AutoDisposableFlow CreateSelfDisposableFlow() => new() { flow = CreateFlow() };
        SkillMasterUnit MasterUnit => SkillMachine.MasterUnit;
        IEnumerable<IPassiveUnit> PassiveUnits(PassiveType type) {
            return SkillMachine.Graph.Units<IPassiveUnit>().Where(unit => unit.Type == type);
        }
        
        // === Properties From Graph

        // -- Availability
        
        public bool IsAvailable {
            get {
                using var flow = CreateFlow();
                return (MasterUnit?.IsAvailable(flow) ?? false);
            }
        }

        // -- Cost
        
        public bool HasCost => (MasterUnit?.HasCost ?? false);
        
        [UnityEngine.Scripting.Preserve]
        ICost ModifiedCost(ICost cost) {
            if (cost is StatCost statCost) {
                statCost = Elements<PassiveCostModifier>()
                    .Where(pcm => !pcm.IsOverride)
                    .Aggregate(statCost, (a, b) => b.OverridenCost(a));

                statCost = Elements<PassiveCostModifier>()
                    .Where(pcm => pcm.IsOverride)
                    .Aggregate(statCost, (a, b) => b.OverridenCost(a));

                return statCost;
            } else {
                return cost;
            }
        }
        public ICost Cost {
            get {
                if (MasterUnit.HasCost) {
                    using var flow = CreateFlow();
                    var cost = MasterUnit.GetCost(flow);
                    return cost;
                }
                return NoCost.Instance;
            }
        }

        // -- Cooldown
        
        public bool HasCooldown => MasterUnit.HasCooldown;
        public ISkillCooldown Cooldown {
            get {
                using var flow = CreateFlow();
                return MasterUnit.GetCooldown(flow);
            }
        }

        public bool IsCoolingDown => HasElement<ISkillCooldown>();
        
        // -- Tooltip
        
        public string TooltipPart {
            get {
                if (MasterUnit.HasTooltip) {
                    using var flow = CreateFlow();
                    return MasterUnit.GetTooltip(flow);
                }
                return null;
            }
        }
        
        // === Effects

        public bool CanSubmit => IsAvailable && !IsCoolingDown && (!HasCost || Cost.CanAfford()) && !HasBeenDiscarded;

        void EnablePassiveUnits(PassiveType type) {
            foreach (var unit in PassiveUnits(type)) {
                try {
                    using var flow = CreateFlow();
                    unit.Enable(this, flow);
                } catch (Exception e) {
                    LogException($"on enabling passive unit {unit} on {type}", e);
                }
            }
        }

        void DisablePassiveUnits(PassiveType type) {
            if (_vSkillMachine == null) {
                return;
            }
            foreach (var unit in PassiveUnits(type)) {
                try {
                    using var flow = CreateFlow();
                    unit.Disable(this, flow);
                } catch (Exception e) {
                    LogException($"on disabling passive unit {unit} on {type}", e);
                }
            }
        }

        void RunEffects(string type, ControlOutput effects) {
            if (effects == null) {
                return;
            }
            try {
                SafeGraph.RunRethrow(CreateSelfDisposableFlow(), effects);
            } catch (Exception e) {
                LogException(type, e);
            }
        }

        void LogException(string happenedOn, Exception e) {
            Log.Important?.Error($"Exception below happened {happenedOn} in skill {LogUtils.GetDebugName(this)} with ParentModel -> {LogUtils.GetDebugName(ParentModel)}", Graph);
            Debug.LogException(e, _vSkillMachine?.Machine);
        }

        
        // -- Learn

        public void Learn() {
            if (ShouldNotEnterState || IsLearned) {
                return;
            }
            _isLearned = true;
            OnLearn();
        }
        void OnLearn() {
            RunEffects(LearnEffects, MasterUnit.OnLearn);
            OnLearnRepetitive();
            TriggerChange();
        }

        void OnLearnRepetitive() {
            RunEffects(LearnRepetitiveEffects, MasterUnit.OnLearnRepetitive);
            EnablePassiveUnits(PassiveType.Learn);
            if (Owner is not null) {
                Owner.ListenTo(Model.Events.BeforeDiscarded, Forget, this);
            } else {
                Log.Critical?.Error($"Owner is null for skill {this} ParentModel -> {LogUtils.GetDebugName(ParentModel)}");
            }
        }

        public void Forget() {
            if (ShouldNotExitState || !IsLearned) {
                return;
            }
            Cancel();
            Unequip();
            _isLearned = false;
            OnForget();
        }
        void OnForget() {
            OnForgetRepetitive();
            RunEffects(ForgetEffects, MasterUnit.OnForget);
            TriggerChange();
        }

        void OnForgetRepetitive() {
            DisablePassiveUnits(PassiveType.Learn);
            RunEffects(ForgetRepetitiveEffects, MasterUnit.OnForgetRepetitive);
        }

        // -- Equip

        public void Equip() {
            if (ShouldNotEnterState || IsEquipped) {
                return;
            }
            _isEquipped = true;
            OnEquip();
        }
        void OnEquip() {
            RunEffects(EquipEffects, MasterUnit.OnEquip);
            OnEquipRepetitive();
            TriggerChange();
        }
        
        void OnEquipRepetitive() {
            RunEffects(EquipRepetitiveEffects, MasterUnit.OnEquipRepetitive);
            EnablePassiveUnits(PassiveType.Equip);
        }

        public void Unequip() {
            if (ShouldNotExitState || !IsEquipped) {
                return;
            }
            _isEquipped = false;
            OnUnequip();
        }
        void OnUnequip() {
            OnUnequipRepetitive();
            RunEffects(UnEquipEffects, MasterUnit.OnUnequip);
            TriggerChange();
        }

        void OnUnequipRepetitive() {
            DisablePassiveUnits(PassiveType.Equip);
            RunEffects(UnEquipRepetitiveEffects, MasterUnit.OnUnequipRepetitive);
        }
        
        // -- Activate

        public void Submit() {
            if (ShouldNotEnterState || IsSubmitted) {
                return;
            }
            _isSubmitted = true;
            OnSubmit();
        }
        void OnSubmit() {
            RunEffects(SubmitEffects, MasterUnit.OnSubmit);
            OnSubmitRepetitive();
            
            if (HasCost) {
                ReservedCosts.Clear();
                Cost.Pay();
            }
            
            if (HasCooldown) {
                Cooldown?.ApplyOn(this);
            }
            
            TriggerChange();
        }

        void OnSubmitRepetitive() {
            RunEffects(SubmitRepetitiveEffects, MasterUnit.OnSubmitRepetitive);
            EnablePassiveUnits(PassiveType.Submit);
        }

        public void Perform() {
            if (ShouldNotEnterState || !IsSubmitted) {
                return;
            }
            
            OnPerformed();
        }

        void OnPerformed() {
            RunEffects(PerformEffects, MasterUnit.OnPerform);
        }

        public void Cancel() {
            if (ShouldNotExitState || !IsSubmitted) {
                return;
            }
            _isSubmitted = false;
            OnCancel();
        }
        
        void OnCancel() {
            OnCancelRepetitive();
            RunEffects(CancelEffects, MasterUnit.OnCancel);
            TriggerChange();
        }

        void OnCancelRepetitive() {
            DisablePassiveUnits(PassiveType.Submit);
            RunEffects(CancelRepetitiveEffects, MasterUnit.OnCancelRepetitive);
        }

        public void Refund() {
            if (HasCost) {
                Cost.Refund();
                if (ReservedCosts.Any()) {
                    new TotalCost(ReservedCosts).Refund();
                    ReservedCosts.Clear();
                }
            }
        }
        
        // --- Charging
        public void ChargeStepIncrease(int value) {
            try {
                var flow = CreateSelfDisposableFlow();
                flow.flow.SetValue(MasterUnit.ChargeStepValue, value);
                SafeGraph.RunRethrow(flow, MasterUnit.OnChargeStepIncrease);
            } catch (Exception e) {
                LogException(ChargeStepIncreaseEffects, e);
            }
        }
        
        public void ChargeLevelIncrease(float value) {
            try {
                var flow = CreateSelfDisposableFlow();
                flow.flow.SetValue(MasterUnit.ChargeLevelValue, value);
                SafeGraph.RunRethrow(flow, MasterUnit.OnChargeLevelIncrease);
            } catch (Exception e) {
                LogException(ChargeLevelIncreaseEffects, e);
            }
        }

        // === Variables

        public IEnumerable<string> VariableNames() {
            return Graph.AllVariableNames;
        }

        public float? GetVariable(string id, int index, ICharacter owner = null) => GetVariable(id, owner);
        public StatType GetEnum(string id, int index) => GetRichEnum(id);

        public void OverrideVariable(string name, float value) {
            _variableOverrides ??= new List<SkillVariable>();
            for (int i = 0; i < _variableOverrides.Count; i++) {
                if (_variableOverrides[i].name == name) {
                    _variableOverrides[i].value = value;
                    return;
                }
            }
            _variableOverrides.Add(new SkillVariable(name, value));
        }

        SkillVariable GetVariableOverride(string name) {
            Func<SkillVariable, bool> predicate = v => v.name == name;
            return _variableOverrides?.FirstOrDefault(predicate) 
                   ?? _variables.FirstOrDefault(predicate) 
                   ?? Graph.SkillVariables.FirstOrDefault(predicate);
        }
        
        public float? GetVariable(string name, [CanBeNull] ICharacter owner) {
            // Getting computable invokes VS, which might reach for the owner.
            // We want to be able to override owner in order to construct description for Hero even if Skill doesn't belong to Hero yet.
            _tempOwnerOverride = owner;
            float? result = GetComputable(name) ?? GetVariableOverride(name)?.value;
            _tempOwnerOverride = null;
            return result;
        }

        public void OverrideRichEnum(string name, StatType statType) {
            _enumOverrides ??= new List<SkillRichEnum>();
            var reference = new RichEnumReference(statType);
            for (int i = 0; i < _enumOverrides.Count; i++) {
                if (_enumOverrides[i].name == name) {
                    _enumOverrides[i].enumReference = reference;
                    return;
                }
            }
            _enumOverrides.Add(new SkillRichEnum(name, reference));
        }
        
        SkillRichEnum GetRichEnumOverride(string name) {
            Func<SkillRichEnum, bool> predicate = e => e.name == name;
            return _enumOverrides?.FirstOrDefault(predicate)
                   ?? _enums.FirstOrDefault(predicate)
                   ?? Graph.SkillEnums.FirstOrDefault(predicate);
        }
        public StatType GetRichEnum(string name) {
            var enumOverride = GetRichEnumOverride(name);
            return enumOverride?.Value;
        }
        
        public void OverrideDatum(string name, VSDatumType type, VSDatumValue value) {
            _datumOverrides ??= new List<SkillDatum>();
            for (int i = 0; i < _datumOverrides.Count; i++) {
                var variable = _datumOverrides[i];
                if (variable.name == name) {
                    variable.type = type;
                    variable.value = value;
                    _datumOverrides[i] = variable;
                    return;
                }
            }
            _datumOverrides.Add(new SkillDatum(name, type, value));
        }
        
        SkillDatum? GetDatumOverride(string name, in VSDatumType type) {
            if (_datumOverrides != null) {
                foreach (var variable in _datumOverrides) {
                    if (variable.name == name && variable.type.Equals(type)) {
                        return variable;
                    }
                }
            }
            foreach (var variable in _datums) {
                if (variable.name == name && variable.type.Equals(type)) {
                    return variable;
                }
            }
            foreach (var variable in Graph.SkillDatums) {
                if (variable.name == name && variable.type.Equals(type)) {
                    return variable;
                }
            }
            return null;
        }
        
        public VSDatumValue? GetDatum(string name, in VSDatumType type) {
            var datumOverride = GetDatumOverride(name, type);
            return datumOverride?.value;
        }
        
        // === Computables
        public void RegisterComputable(string id, Func<float> func) {
            if (!_computables.ContainsKey(id)) {
                _computables.Add(id, new SkillComputable(id, func));
            } else {
                Log.Important?.Error($"Trying to add duplicated Computable Value with id: {id} in Skill {Graph?.name}", Graph);
            }
        }

        float? GetComputable(string id) {
            if (_computables.TryGetValue(id, out SkillComputable cv)) {
                return cv.ValueFunc();
            }
            return null;
        }
        
        // === Assets
        SkillAssetReference GetAssetReferenceOverride(string name) {
            return _assetReferences.FirstOrDefault(e => e.name == name) 
                   ?? Graph.SkillAssetReferences.FirstOrDefault(e => e.name == name);
        }

        public ARAssetReference GetAssetReference(string name) {
            return GetAssetReferenceOverride(name)?.ARAssetReference;
        }
        
        public ShareableARAssetReference GetShareableAssetReference(string name) {
            return GetAssetReferenceOverride(name)?.ShareableARAssetReference;
        }
        
        // === Templates
        SkillTemplate GetTemplateOverride(string name) {
            return _templates.FirstOrDefault(e => e.name == name) ?? Graph.SkillTemplates.FirstOrDefault(e => e.name == name);
        }
        
        public TemplateWrapper<T> GetTemplate<T>(string name) where T : class, ITemplate {
            var templateReference = GetTemplateOverride(name)?.templateReference;
            return templateReference != null ? new TemplateWrapper<T>(templateReference.Get<T>()) : null;
        }

        public TemplateReference GetTemplateReference(string name) {
            return GetTemplateOverride(name)?.templateReference;
        }

        // === Refreshing

        public void Refresh() {
            _description = SkillsUtils.ConstructDescriptionToken(this);
            _tooltip = SkillsUtils.ConstructTooltip(this, _description);
        }

        // === Events
        
        public new class Events {
            public static readonly Event<Skill, Skill> ContextChanged = new(nameof(ContextChanged));
            public static readonly Event<CharacterSkills, Skill> SkillLearned = new(nameof(SkillLearned));
            public static readonly Event<CharacterSkills, Skill> SkillForgot = new(nameof(SkillForgot));
            public static readonly Event<Skill, ISkillCooldown> CooldownAdded = new(nameof(CooldownAdded));
        }
    }
}