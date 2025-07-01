using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Buffs {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that apply buffs to items, used by coatings.")]
    public class ItemBuffApplierAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] List<SkillReference> skills = new();
        [SerializeField, LabelText("Duration[s]")] float duration;
        [SerializeField, LabelText("Duration Gain [s/level]")] float durationGain;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        ShareableARAssetReference weaponVFX;
        
        public IEnumerable<SkillReference> Skills => skills;
        public ShareableARAssetReference VFX => weaponVFX;
        public float Duration(int level) => duration + durationGain * level;
        
        public Element SpawnElement() {
            return new ItemBuffApplier();
        }

        public bool IsMine(Element element) => element is ItemBuffApplier;
    }
}