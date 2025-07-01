using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For items that spawn projectiles and have multiple levels of them.")]
    public class UpgradeableItemProjectileAttachment : MonoBehaviour, IAttachmentSpec {
        public ItemProjectileAttachment.ItemProjectileData[] projectilesData = Array.Empty<ItemProjectileAttachment.ItemProjectileData>();
        
        public IEnumerable<SkillReference> Skills => projectilesData.Length > 0 ? projectilesData[0].skills : null;
        
        public Element SpawnElement() {
            return new UpgradeableItemProjectile();
        }

        public bool IsMine(Element element) => element is UpgradeableItemProjectile;
    }
}
