using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("SkillVariables")]
    [UnityEngine.Scripting.Preserve]
    public class SkillVariableOverrideUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int variablesCount;
        [Serialize, Inspectable, UnitHeaderInspectable] public int enumsCount;
        [Serialize, Inspectable, UnitHeaderInspectable] public int datumsCount;
        [Serialize, Inspectable, UnitHeaderInspectable] public int assetReferencesCount;
        [Serialize, Inspectable, UnitHeaderInspectable] public int templatesCount;

        InlineValueInput<string>[] _enumNames;
        RequiredValueInput<StatType>[] _enumValues;
        
        InlineValueInput<string>[] _variablesNames;
        InlineValueInput<float>[] _variablesValues;
        
        InlineValueInput<string>[] _datumsNames;
        RequiredValueInput<VSDatumType>[] _datumsTypes;
        RequiredValueInput<VSDatumValue>[] _datumsValues;
        
        InlineValueInput<string>[] _assetReferencesNames;
        RequiredValueInput<ShareableARAssetReference>[] _assetReferencesValues;
        
        InlineValueInput<string>[] _templatesNames;
        RequiredValueInput<TemplateReference>[] _templatesValues;
        
        protected override void Definition() {
            _variablesNames = new InlineValueInput<string>[variablesCount];
            _variablesValues = new InlineValueInput<float>[variablesCount];
            for (int i = 0; i < variablesCount; i++) {
                _variablesNames[i] = InlineARValueInput($"variable {i} name", "");
                _variablesValues[i] = InlineARValueInput<float>($"variable {i} value", 0);
            }
            
            _enumNames = new InlineValueInput<string>[enumsCount];
            _enumValues = new RequiredValueInput<StatType>[enumsCount];
            for (int i = 0; i < enumsCount; i++) {
                _enumNames[i] = InlineARValueInput($"enum {i} name", "");
                _enumValues[i] = RequiredARValueInput<StatType>($"enum {i} value");
            }
            
            _datumsNames = new InlineValueInput<string>[datumsCount];
            _datumsTypes = new RequiredValueInput<VSDatumType>[datumsCount];
            _datumsValues = new RequiredValueInput<VSDatumValue>[datumsCount];
            for (int i = 0; i < datumsCount; i++) {
                _datumsNames[i] = InlineARValueInput($"datum {i} name", "");
                _datumsTypes[i] = RequiredARValueInput<VSDatumType>($"datum {i} type");
                _datumsValues[i] = RequiredARValueInput<VSDatumValue>($"datum {i} value");
            }
            
            _assetReferencesNames = new InlineValueInput<string>[assetReferencesCount];
            _assetReferencesValues = new RequiredValueInput<ShareableARAssetReference>[assetReferencesCount];
            for (int i = 0; i < assetReferencesCount; i++) {
                _assetReferencesNames[i] = InlineARValueInput($"asset ref {i} name", "");
                _assetReferencesValues[i] = RequiredARValueInput<ShareableARAssetReference>($"asset ref {i} value");
            }
            
            _templatesNames = new InlineValueInput<string>[templatesCount];
            _templatesValues = new RequiredValueInput<TemplateReference>[templatesCount];
            for (int i = 0; i < templatesCount; i++) {
                _templatesNames[i] = InlineARValueInput($"templates {i} name", "");
                _templatesValues[i] = RequiredARValueInput<TemplateReference>($"templates {i} value");
            }

            ValueOutput("variables", flow => new SkillVariablesOverride(Variables(flow), Enums(flow), Datums(flow), AssetRefs(flow), Templates(flow)));
        }

        SkillVariable[] Variables(Flow flow) {
            var result = new SkillVariable[variablesCount];
            for (int i = 0; i < variablesCount; i++) {
                result[i] = new SkillVariable(_variablesNames[i].Value(flow), _variablesValues[i].Value(flow));
            }
            return result;
        }
        
        SkillRichEnum[] Enums(Flow flow) {
            var result = new SkillRichEnum[enumsCount];
            for (int i = 0; i < enumsCount; i++) {
                result[i] = new SkillRichEnum(_enumNames[i].Value(flow), _enumValues[i].Value(flow));
            }
            return result;
        }
        
        SkillDatum[] Datums(Flow flow) {
            var result = new SkillDatum[datumsCount];
            for (int i = 0; i < datumsCount; i++) {
                result[i] = new SkillDatum(_datumsNames[i].Value(flow), _datumsTypes[i].Value(flow), _datumsValues[i].Value(flow));
            }
            return result;
        }
        
        SkillAssetReference[] AssetRefs(Flow flow) {
            var result = new SkillAssetReference[assetReferencesCount];
            for (int i = 0; i < assetReferencesCount; i++) {
                result[i] = new SkillAssetReference(_assetReferencesNames[i].Value(flow), _assetReferencesValues[i].Value(flow));
            }
            return result;
        }
        
        SkillTemplate[] Templates(Flow flow) {
            var result = new SkillTemplate[templatesCount];
            for (int i = 0; i < templatesCount; i++) {
                result[i] = new SkillTemplate(_templatesNames[i].Value(flow), _templatesValues[i].Value(flow));
            }
            return result;
        }
    }
}