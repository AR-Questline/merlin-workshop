using System;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tools {
    [Serializable]
    public class ItemLootData {
        [TemplateType(typeof(ItemTemplate)), HideInInspector, SerializeField]
        public TemplateReference template;

        [InlineButton(nameof(PingTemplate), "Ping"), DisplayAsString] [UnityEngine.Scripting.Preserve] 
        public string name;

        [TableColumnWidth(90, false)]
        public int minCount;

        [TableColumnWidth(90, false)]
        public int maxCount;

        public float probability;
        
        public BitMaskIndex tags;

        public bool Grindable {
            get => tags.HasFlagFast(BitMaskIndex.Grindable);
            set => Set(ref tags, BitMaskIndex.Grindable, value);
        }
        public bool OnlyNight {
            get => tags.HasFlagFast(BitMaskIndex.OnlyNight);
            set => Set(ref tags, BitMaskIndex.OnlyNight, value);
        }
        public bool Conditional {
            get => tags.HasFlagFast(BitMaskIndex.Conditional);
            set => Set(ref tags, BitMaskIndex.Conditional, value);
        }
        public bool OwnedByNpc {
            get => tags.HasFlagFast(BitMaskIndex.OwnedByNpc);
            set => Set(ref tags, BitMaskIndex.OwnedByNpc, value);
        }
        public bool AffectedByLootChanceMultiplier {
            get => tags.HasFlagFast(BitMaskIndex.AffectedByLootChanceMultiplier);
            set => Set(ref tags, BitMaskIndex.AffectedByLootChanceMultiplier, value);
        }
        
        public bool IsStealable { 
            get => tags.HasFlagFast(BitMaskIndex.IsStealable);
            set => Set(ref tags, BitMaskIndex.IsStealable, value);
    }

        public IntRange IntRange => new(minCount, maxCount);

        ItemTemplate _template;
        public ItemTemplate Template {
            get {
                try {
                    return _template ??= template.Get<ItemTemplate>();
                } catch {
                    return null;
                }
            }
        }

        public ItemLootData(TemplateReference template, int count = 1, float probability = 1f, bool grindable = false) {
            this.template = template;
            this.minCount = count;
            this.maxCount = count;
            this.probability = probability;
            Grindable = grindable;
            name = Template?.name;
        }

        public ItemLootData(TemplateReference template, int minCount, int maxCount, float probability = 1f, bool grindable = false) : this(template, minCount,
            probability, grindable) {
            this.maxCount = maxCount;
        }

        void PingTemplate() {
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(Template.gameObject);
#endif
        }
        
        static void Set(ref BitMaskIndex value, BitMaskIndex flag, bool set) {
            value = set ? value | flag : value & ~flag;
        }
        
        [Flags]
        public enum BitMaskIndex : byte {
            Grindable = 1 << 0,
            OnlyNight = 1 << 1,
            Conditional = 1 << 2,
            OwnedByNpc = 1 << 3,
            AffectedByLootChanceMultiplier = 1 << 4,
            IsStealable = 1 << 5
        }
    }
}