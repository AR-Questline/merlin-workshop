using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern.Host;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Subtree;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.SerializableTypeReference;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public class TalentTreeTemplate : ScriptableObject, ITemplate {
        [SerializeField, LocStringCategory(Category.UI)] LocString displayName;
        [SerializeField, Tags(TagsCategory.Flag)] string requiredFlag = "";

        [SerializeField, RichEnumExtends(typeof(StatType))]
        RichEnumReference currencyStatType;
        [SerializeField, UIAssetReference(AddressableLabels.UI.Talents), ShowAssetPreview]
        ShareableSpriteReference icon;
        [SerializeField]
        bool showMainIcon = true;
        [SerializeField]
        bool showSubtreeIcon = true;
        [SerializeField] 
        List<TalentSubTree> treeSubTrees = new(); 
        [SerializeField, TypeDrawerSettings(BaseType = typeof(VTalentTreePatternHost))] 
        SerializableTypeReference patternType;
        
        [SerializeField, HideInInspector] TemplateMetadata metadata;

        public string GUID { get; set; }
        public ShareableSpriteReference Icon => icon;
        public string Name => displayName;
        public string RequiredFlag => requiredFlag;
        public StatType CurrencyStatType => currencyStatType.EnumAs<StatType>();
        public Type PatternType => patternType.Type;
        public TemplateMetadata Metadata => metadata;
        public List<TalentSubTree> TreeSubTrees => treeSubTrees;
        public bool ShowMainIcon => showMainIcon;
        public bool ShowSubtreeIcon => showSubtreeIcon;

        public List<TalentTreeNode> TalentNodes => _cachedTalentNodes ??= treeSubTrees.SelectMany(subTree => subTree.TreeNodes).ToList();
        List<TalentTreeNode> _cachedTalentNodes;

        string INamed.DisplayName => Name;
        string INamed.DebugName => name;
        
        void OnEnable() {
            _cachedTalentNodes = null;
        }
        
#if UNITY_EDITOR
        [Button]
        void GenerateTalentTreeStructureBasedOnPattern(VTalentTreePatternBase patternBase) {
            var currencyCache = new Dictionary<TalentSubtreeType, (bool overrideCurrency, StatType currency)>();
            foreach (var existing in treeSubTrees) {
                currencyCache[existing.SubtreeType] = (existing.OverrideCurrencyStatType, existing.CurrencyStatType);
            }

            treeSubTrees.Clear();
            _cachedTalentNodes = null;
            var subtreeMap = new Dictionary<TalentSubtreeType, TalentSubTree>();

            foreach (TalentSubtreeType key in patternBase.SubTrees.Select(patternSubtree => patternSubtree.SubtreeType).Where(key => !subtreeMap.ContainsKey(key))) {
                var subtree = currencyCache.TryGetValue(key, out var cached)
                    ? new TalentSubTree(key, cached.overrideCurrency, cached.currency)
                    : new TalentSubTree(key);
                subtreeMap.Add(key, subtree);
                treeSubTrees.Add(subtree);
            }

            foreach (var patternSlot in patternBase.CachedSlots) {
                subtreeMap[patternSlot.SubtreeType].TreeNodes.Add(new TalentTreeNode(patternSlot));
            }
        }
#endif

        [Serializable]
        public class TalentSubTree {
            [SerializeField, RichEnumExtends(typeof(TalentSubtreeType))] 
            RichEnumReference subtreeType;
            [SerializeField] 
            bool overrideCurrencyStatType;
            [SerializeField, RichEnumExtends(typeof(StatType)), CanBeNull, ShowIf(nameof(overrideCurrencyStatType))]
            RichEnumReference currencyStatType;
            [SerializeField, ReadOnly] 
            List<TalentTreeNode> treeNodes = new();
            
            public bool OverrideCurrencyStatType => overrideCurrencyStatType;
            public TalentSubtreeType SubtreeType => subtreeType.EnumAs<TalentSubtreeType>();
            public StatType CurrencyStatType => currencyStatType?.EnumAs<StatType>();
            public List<TalentTreeNode> TreeNodes => treeNodes;

            public TalentSubTree(TalentSubtreeType subtreeType) {
                this.subtreeType = new RichEnumReference(subtreeType);
            }
            
            public TalentSubTree(TalentSubtreeType subtreeType, bool overrideCurrencyStatType, StatType currencyStatType) {
                this.subtreeType = new RichEnumReference(subtreeType);
                this.overrideCurrencyStatType = overrideCurrencyStatType;
                this.currencyStatType = new RichEnumReference(currencyStatType);
            }
        }

        [Serializable]
        public struct TalentTreeNode {
            [SerializeField, TemplateType(typeof(TalentTemplate))]
            TemplateReference talentReference;
            [SerializeField] 
            bool isRoot;
            [SerializeField, TemplateType(typeof(TalentTemplate)), CanBeNull, HideIf(nameof(isRoot))]
            TemplateReference parentTalent;
            
            public TalentTemplate Talent => talentReference.Get<TalentTemplate>();
            public TalentTemplate Parent => isRoot ? null : parentTalent?.Get<TalentTemplate>();
            
            public TalentTreeNode(TalentPatternSlot patternSlot) {
                talentReference = patternSlot.TalentReference;
                isRoot = !patternSlot.HasParent;
                parentTalent = patternSlot.ParentTalentReference;
            }
        }
    }
}
