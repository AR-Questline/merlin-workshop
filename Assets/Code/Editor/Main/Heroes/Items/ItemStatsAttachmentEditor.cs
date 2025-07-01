using System.Linq;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Heroes.Items {
    [CustomEditor(typeof(ItemStatsAttachment)), CanEditMultipleObjects]
    public class ItemStatsAttachmentEditor : OdinEditor {
        ItemStatsAttachment[] _targets;
        ItemTemplate[] _templates;
        ItemStatsAttachment _current;
        ItemTemplate _template;
        
        protected override void OnEnable() {
            base.OnEnable();
            _targets = new ItemStatsAttachment[targets.Length];
            _templates = new ItemTemplate[targets.Length];
            
            for (int i = 0; i < _targets.Length; i++) {
                _targets[i] = (ItemStatsAttachment)targets[i];
                _templates[i] = _targets[i].GetComponent<ItemTemplate>();
            }
        }
        
        public override void OnInspectorGUI() {
            // Handle multi Editing update
            for (int index = 0; index < _targets.Length; index++) {
                _current = _targets[index];
                _template = _templates[index];
                MultiUpdate();
            }
            
            if (_templates[0].AbstractTypes.CheckIsEmptyAndRelease()) {
                EditorGUILayout.HelpBox("ItemTemplate is missing an abstract. For this attachment to work you need abstract attached", MessageType.Error);
            } else {
                base.OnInspectorGUI();
            }
        }

        void MultiUpdate() {
            if (IsWeapon(_template)) {
                // Ensure that base damage subtype is valid
                ValidateDamageTypes();
            }
        }

        void ValidateDamageTypes() {
            if (_current.damageSubTypes.Count == 0) {
                _current.damageSubTypes.Add(new DamageTypeDataConfig {subType = DamageSubType.Default, percentage = 100, calculatedPercentage = true});
                EditorUtility.SetDirty(this);
                return;
            }
            // calculate percentage for base damage type
            DamageTypeDataConfig additionalDamageSubType = _current.damageSubTypes[0];
            int newLeftoverPercent = 100 - _current.damageSubTypes.Skip(1).Sum(s => s.percentage);
            if (newLeftoverPercent == additionalDamageSubType.percentage) return;
            
            additionalDamageSubType.percentage = newLeftoverPercent;
            _current.damageSubTypes[0] = additionalDamageSubType;
            EditorUtility.SetDirty(_current);
        }
        
        bool IsWeapon(ItemTemplate template) => template.IsWeapon || template.IsArrow || template.IsThrowable;
    }
}