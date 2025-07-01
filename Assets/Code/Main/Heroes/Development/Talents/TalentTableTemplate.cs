using System;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public class TalentTableTemplate : ScriptableObject, ITemplate {
        public string GUID { get; set; }
        
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata => metadata;

        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
        
        [SerializeField, LocStringCategory(Category.UI)] LocString displayName;
        [SerializeField, TableList(IsReadOnly = true, CellPadding = 20)] Row[] table = new Row[3];
        [SerializeField, RichEnumExtends(typeof(HeroRPGStatType))] 
        RichEnumReference heroStatTypeReference;

        [UnityEngine.Scripting.Preserve] public HeroStatType HeroStatType => heroStatTypeReference.EnumAs<HeroStatType>();
        [UnityEngine.Scripting.Preserve] public string Name => displayName;
        [UnityEngine.Scripting.Preserve] public ref Row this[int row] => ref table[row];
        [UnityEngine.Scripting.Preserve] public int RequiredStatLevelToUnlock(int row) => table[row].RequiredStatLevelToUnlock;
        
        [Serializable]
        public struct Row {
            [SerializeField, TemplateType(typeof(TalentTemplate)), HideLabel, VerticalGroup("Left")] TemplateReference talent0;
            [SerializeField, TemplateType(typeof(TalentTemplate)), HideLabel, VerticalGroup("Middle")] TemplateReference talent1;
            [SerializeField, TemplateType(typeof(TalentTemplate)), HideLabel, VerticalGroup("Right")] TemplateReference talent2;
            [SerializeField, HideLabel, VerticalGroup("Required Level"), Tooltip("Required HeroRPGStat to unlock")]
            int requiredStatLevelToUnlock;

            public int RequiredStatLevelToUnlock => requiredStatLevelToUnlock;

            [UnityEngine.Scripting.Preserve]
            public TalentTemplate this[int column] { get {
                var reference = column switch {
                    0 => talent0,
                    1 => talent1,
                    2 => talent2,
                    _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
                };
                return reference.Get<TalentTemplate>();
            }}
        }
    }
}