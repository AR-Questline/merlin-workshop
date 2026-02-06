using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Create Skill Datum Data")]
    [UnityEngine.Scripting.Preserve]
    public class CreateSkillDatumDataUnit : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VSDatumType type;
        
        ARValueInput<bool> boolValue;
        ARValueInput<int> intValue;
        ARValueInput<ARAssetReference> assetValue;
        ARValueInput<string> stringValue;
        ARValueInput<StatusType> statusTypeValue;
        ARValueInput<HeroRPGStatType> rpgStatTypeValue;
        ARValueInput<DamageType> damageTypeValue;
        ARValueInput<DamageSubType> damageSubTypeValue;
        ARValueInput<TweakPriority> tweakPriorityValue;
        protected override void Definition() {
            SetupInput();
            ValueOutput(typeof(VSDatumType), "type", _ => type);
            ValueOutput(typeof(VSDatumValue), "value", flow => CreateDatumValueOutput(flow));
        }

        void SetupInput() {
            switch (type.general) {
                case VSDatumGeneralType.Bool:
                    boolValue = InlineARValueInput<bool>("value", false);
                    break;
                case VSDatumGeneralType.Int:
                    intValue = InlineARValueInput<int>("value", 0);
                    break;
                case VSDatumGeneralType.RichEnum:
                    SetupRichEnumInput();
                    break;
                case VSDatumGeneralType.Enum:
                    SetupEnumInput();
                    break;
                case VSDatumGeneralType.String:
                    stringValue = InlineARValueInput<string>("value", "");
                    break;
                case VSDatumGeneralType.Asset:
                    assetValue = InlineARValueInput<ARAssetReference>("value", null);
                    break;
                case VSDatumGeneralType.Flag:
                    throw new ArgumentOutOfRangeException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        void SetupRichEnumInput() {
            switch ((VSDatumRichEnumType) type.specific) { 
                case VSDatumRichEnumType.StatusType:
                    statusTypeValue = RequiredARValueInput<StatusType>("value");
                    break;
                case VSDatumRichEnumType.RPGStats:
                    rpgStatTypeValue = RequiredARValueInput<HeroRPGStatType>("value");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void SetupEnumInput() {
            switch ((VSDatumEnumType) type.specific) { 
                case VSDatumEnumType.DamageType:
                    damageTypeValue = RequiredARValueInput<DamageType>("value");
                    break;
                case VSDatumEnumType.DamageSubtype:
                    damageSubTypeValue = RequiredARValueInput<DamageSubType>("value");
                    break;
                case VSDatumEnumType.TweakPriority: {
                    tweakPriorityValue = RequiredARValueInput<TweakPriority>("value");
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        VSDatumValue CreateDatumValueOutput(Flow flow) {
            return type.general switch {
                VSDatumGeneralType.Bool => new VSDatumValue() { Bool = boolValue.Value(flow) },
                VSDatumGeneralType.Int => new VSDatumValue() { Int = intValue.Value(flow) },
                VSDatumGeneralType.RichEnum => CreateRichEnumOutput(flow),
                VSDatumGeneralType.Enum => CreateEnumOutput(flow),
                VSDatumGeneralType.String => new VSDatumValue() { String = stringValue.Value(flow) },
                VSDatumGeneralType.Asset => new VSDatumValue() { Asset = assetValue.Value(flow) },
                VSDatumGeneralType.Flag => throw new ArgumentOutOfRangeException(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        VSDatumValue CreateRichEnumOutput(Flow flow) {
            return (VSDatumRichEnumType)type.specific switch {
                VSDatumRichEnumType.StatusType => new VSDatumValue() { RichEnum = statusTypeValue.Value(flow) },
                VSDatumRichEnumType.RPGStats => new VSDatumValue() { RichEnum = rpgStatTypeValue.Value(flow) },
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        VSDatumValue CreateEnumOutput(Flow flow) {
            return (VSDatumEnumType)type.specific switch {
                VSDatumEnumType.DamageType => new VSDatumValue() { Int = (int)damageTypeValue.Value(flow) },
                VSDatumEnumType.DamageSubtype => new VSDatumValue() { Int = (int)damageSubTypeValue.Value(flow) },
                VSDatumEnumType.TweakPriority => new VSDatumValue() { Int = (int)tweakPriorityValue.Value(flow) },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}