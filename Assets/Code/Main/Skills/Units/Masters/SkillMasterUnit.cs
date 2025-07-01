using System;
using System.Collections.Generic;
using Awaken.TG.Main.General.Costs;
using Awaken.TG.Main.Skills.Cooldowns;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Masters {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    public class SkillMasterUnit : Unit, ISkillUnit, ICustomOutputOrderUnit {

        [Serialize, Inspectable] bool learn = true; 
        [Serialize, Inspectable] bool learnRepetitive;
        [Serialize, Inspectable] bool equip = true;
        [Serialize, Inspectable] bool equipRepetitive;
        [Serialize, Inspectable] bool submit = true;
        [Serialize, Inspectable] bool submitRepetitive;
        [Serialize, Inspectable] bool chargeStepIncrease = true;
        [Serialize, Inspectable] bool chargeLevelIncrease = true;
        
        ValueInput _availability;
        ValueInput _cost;
        ValueInput _cooldown;
        ValueInput _tooltip;

        ControlOutput _onSubmit;
        ControlOutput _onPerform;
        ControlOutput _onCancel;
        ControlOutput _onLearn;
        ControlOutput _onForget;
        ControlOutput _onEquip;
        ControlOutput _onUnequip;
        
        ControlOutput _onChargeStepIncrease;
        ControlOutput _onChargeLevelIncrease;
        ValueOutput _chargeStep;
        ValueOutput _chargeLevel;
        
        ControlOutput _onSubmitRepetitive;
        ControlOutput _onCancelRepetitive;
        ControlOutput _onLearnRepetitive;
        ControlOutput _onForgetRepetitive;
        ControlOutput _onEquipRepetitive;
        ControlOutput _onUnequipRepetitive;
        
        public override bool isControlRoot => true;

        public IEnumerable<IUnitOutputPort> OrderedOutputs {
            get {
                yield return _onSubmit;
                yield return _onPerform;
                yield return _onCancel;
                yield return _onLearn;
                yield return _onForget;
                yield return _onEquip;
                yield return _onUnequip;
                yield return _onSubmitRepetitive;
                yield return _onCancelRepetitive;
                yield return _onLearnRepetitive;
                yield return _onForgetRepetitive;
                yield return _onEquipRepetitive;
                yield return _onUnequipRepetitive;
                
                yield return _onChargeStepIncrease;
                yield return _chargeStep;
                yield return _onChargeLevelIncrease;
                yield return _chargeLevel;
            }
        }

        [UnityEngine.Scripting.Preserve]
        protected override void Definition() {
            _availability = ValueInput<bool>("availability");
            _cost = ValueInput<ICost>("cost");
            _cooldown = ValueInput<ISkillCooldown>("cooldown");
            _tooltip = ValueInput<string>("tooltip");
            
            _onSubmit = Output(submit, "onSubmit");
            _onSubmitRepetitive = Output(submitRepetitive, "onSubmitRepetitive");
            _onPerform = Output(submit, "onPerform");
            _onCancelRepetitive = Output(submitRepetitive, "onCancelRepetitive");
            _onCancel = Output(submit, "onCancel");
            _onLearn = Output(learn, "onLearn");
            _onLearnRepetitive = Output(learnRepetitive, "onLearnRepetitive");
            _onForgetRepetitive = Output(learnRepetitive, "onForgetRepetitive");
            _onForget = Output(learn, "onForget");
            _onEquip = Output(equip, "onEquip");
            _onEquipRepetitive = Output(equipRepetitive, "onEquipRepetitive");
            _onUnequipRepetitive = Output(equipRepetitive, "onUnequipRepetitive");
            _onUnequip = Output(equip, "onUnequip");


            _onChargeStepIncrease = Output(chargeStepIncrease, "onChargeStepIncrease");
            _chargeStep = OutputValue<int>(chargeStepIncrease, "chargeStep");
            _onChargeLevelIncrease = Output(chargeLevelIncrease, "onChargeLevelIncrease");
            _chargeLevel = OutputValue<float>(chargeLevelIncrease, "chargeLevel");
            
            ControlOutput Output(bool condition, string name) {
                return condition ? ControlOutput(name) : null;
            }
            
            ValueOutput OutputValue<T>(bool condition, string name) {
                return condition ? ValueOutput<T>(name) : null;
            }
        }

        public bool IsAvailable(Flow flow) => !_availability.hasValidConnection || SafeGraph.GetValue<bool>(flow, _availability);
        public bool HasCost => _cost.hasValidConnection;
        public ICost GetCost(Flow flow) => SafeGraph.GetValue<ICost>(flow, _cost);
        public bool HasCooldown => _cooldown.hasValidConnection;
        public ISkillCooldown GetCooldown(Flow flow) => HasCooldown ? SafeGraph.GetValue<ISkillCooldown>(flow, _cooldown) : null;
        
        public bool HasTooltip => _tooltip.hasValidConnection;
        public string GetTooltip(Flow flow) => SafeGraph.GetValue<string>(flow, _tooltip);

        public ControlOutput OnSubmit => _onSubmit;
        public ControlOutput OnPerform => _onPerform;
        public ControlOutput OnCancel => _onCancel;
        public ControlOutput OnEquip => _onEquip;
        public ControlOutput OnUnequip => _onUnequip;
        public ControlOutput OnLearn => _onLearn;
        public ControlOutput OnForget => _onForget;
        
        public ControlOutput OnSubmitRepetitive => _onSubmitRepetitive;
        public ControlOutput OnCancelRepetitive => _onCancelRepetitive;
        public ControlOutput OnEquipRepetitive => _onEquipRepetitive;
        public ControlOutput OnUnequipRepetitive => _onUnequipRepetitive;
        public ControlOutput OnLearnRepetitive => _onLearnRepetitive;
        public ControlOutput OnForgetRepetitive => _onForgetRepetitive;

        public ControlOutput OnChargeStepIncrease => _onChargeStepIncrease;
        public ControlOutput OnChargeLevelIncrease => _onChargeLevelIncrease;
        public ValueOutput ChargeStepValue => _chargeStep;
        public ValueOutput ChargeLevelValue => _chargeLevel;
    }
}