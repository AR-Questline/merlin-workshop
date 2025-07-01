using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Previews;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "For equippable items, contains prefabs and equipment data.")]
    public class ItemEquipSpec : MonoBehaviour, IAttachmentSpec, IARPreviewProvider {
        const int MaxAddedGemSlots = 2;

        [SerializeField] List<ItemRepresentationByNpc> mobItems;

        [RichEnumExtends(typeof(EquipmentType))] [SerializeField]
        RichEnumReference equipmentType;

        [SerializeField] int gemSlots;

        [RichEnumExtends(typeof(FinisherType)), SerializeField, ShowIf(nameof(IsWeapon))]
        RichEnumReference finisherType = FinisherType.None;

        [RichEnumExtends(typeof(HitsToHitStop)), SerializeField, ShowIf(nameof(IsWeapon))]
        RichEnumReference hitsToHitStop = HitsToHitStop.Blunt;

        public EquipmentType EquipmentType => equipmentType.EnumAs<EquipmentType>();
        public FinisherType FinisherType => finisherType.EnumAs<FinisherType>();
        public HitsToHitStop HitsToHitStop => hitsToHitStop.EnumAs<HitsToHitStop>();
        public int MaxGemSlots => gemSlots + MaxAddedGemSlots;
        public int GemSlots => gemSlots;
#if UNITY_EDITOR
        public IReadOnlyList<ItemRepresentationByNpc> EDITOR_MobItems => mobItems;
#endif

        public ItemRepresentationByNpc[] RetrieveMobItemsInstance() {
            var runtimeItems = new ItemRepresentationByNpc[mobItems.Count];
            for (var i = 0; i < mobItems.Count; i++) {
                runtimeItems[i] = mobItems[i].DeepCopy();
            }

            return runtimeItems;
        }

        public Element SpawnElement() {
            return new ItemEquip();
        }

        public bool IsMine(Element element) {
            return element is ItemEquip;
        }

        // === Editor
        ItemTemplate _template;
        ItemTemplate Template => _template ??= GetComponent<ItemTemplate>();

        bool IsWeapon => Template.IsWeapon;

        public IEnumerable<IARRendererPreview> GetPreviews() {
#if UNITY_EDITOR
            var humanoidItems = mobItems.Where(i => !i.AbstractNPCs.Any() || i.AbstractNPCs.Any(n => n.gameObject.name.Contains("Humanoid"))).ToList();
            if (!humanoidItems.Any()) {
                humanoidItems = mobItems;
            }
            
            var prefab = humanoidItems
                .Select(m => m.itemPrefab?.EditorLoad<GameObject>())
                .FirstOrDefault(p => p != null);

            if (prefab != null) {
                foreach (var preview in prefab.GetComponentsInChildren<IARPreviewProvider>()) {
                    foreach (var previewRenderer in preview.GetPreviews()) {
                        yield return previewRenderer;
                    }
                }
            }
#else
            yield break;
#endif
        }
    }

    [Serializable]
    public partial struct ItemRepresentationByNpc {
        public ushort TypeForSerialization => SavedTypes.ItemRepresentationByNpc;

        [SerializeField, TemplateType(typeof(NpcTemplate))]
        [Saved] TemplateReference[] abstractNpcTemplates;

        [SerializeField]
        [Saved] Gender gender;
        
        [SerializeField]
        [Saved] ItemEquipHand hand;

        [ARAssetReferenceSettings(new []{typeof(GameObject)}, true, AddressableGroup.Weapons)]
        [Saved] public ARAssetReference itemPrefab;

        // === Properties
        public IEnumerable<NpcTemplate> AbstractNPCs => abstractNpcTemplates.Select(n => n.Get<NpcTemplate>());
        public Gender Gender => gender;
        public ItemEquipHand Hand => hand;

        // === Copy
        public ItemRepresentationByNpc DeepCopy() {
            return new() {
                abstractNpcTemplates = abstractNpcTemplates,
                gender = gender,
                hand = hand,
                itemPrefab = itemPrefab.DeepCopy(),
            };
        }
    }

    public enum ItemEquipHand : byte {
        None = 0,
        MainHand = 1,
        OffHand = 2,
    }
}