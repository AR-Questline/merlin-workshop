using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Subtree;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern {
    public class TalentPatternSlot : MonoBehaviour {
        [SerializeField, RichEnumExtends(typeof(TalentSubtreeType))] 
        RichEnumReference subtreeType;
        [SerializeField, TemplateType(typeof(TalentTemplate))]
        TemplateReference talentReference;
        [SerializeField] 
        bool isRoot;
        [SerializeField, TemplateType(typeof(TalentTemplate)), CanBeNull, HideIf(nameof(isRoot))]
        TemplateReference parentTalent;
        [SerializeField, CanBeNull, ShowIf(nameof(isRoot))]
        Transform overrideParent;
        
        public TalentSubtreeType SubtreeType => subtreeType.EnumAs<TalentSubtreeType>();
        public TalentTemplate Talent => talentReference.Get<TalentTemplate>();
        public TemplateReference TalentReference => talentReference;
        public TalentTemplate Parent => isRoot ? null : parentTalent?.Get<TalentTemplate>();
        public TemplateReference ParentTalentReference => parentTalent;
        public bool HasParent => !isRoot || overrideParent != null;
        public Transform OverrideParent => overrideParent;
        public Transform UISlot => transform;
    }
}
