using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track kill of specific NPC type with specified weapon type.")]
    public class KillTrackerAttachment : BaseSimpleTrackerAttachment {
        [SerializeField] bool requireEnemyType;
        [SerializeField, TemplateType(typeof(NpcTemplate)), ShowIf(nameof(requireEnemyType))] 
        List<TemplateReference> allowedEnemyTemplates;
        
        [SerializeField] bool requireUsedWeaponType;
        [SerializeField, TemplateType(typeof(ItemTemplate)), ShowIf(nameof(requireUsedWeaponType))] 
        List<TemplateReference> allowedWeaponTemplates;
        
        public IEnumerable<NpcTemplate> AllowedEnemyTemplates => requireEnemyType ? allowedEnemyTemplates.Select(enemy => enemy.Get<NpcTemplate>()) : null;
        public IEnumerable<ItemTemplate> UsedWeaponTemplate => requireUsedWeaponType ? allowedWeaponTemplates.Select(weapon => weapon.Get<ItemTemplate>()) : null;
        
        public override Element SpawnElement() => new KillTracker();
        public override bool IsMine(Element element) => element is KillTracker;
    }
}