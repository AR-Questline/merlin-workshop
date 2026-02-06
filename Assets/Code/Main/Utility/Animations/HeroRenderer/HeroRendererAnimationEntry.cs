using System;
using Animancer;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Utility.Animations.HeroRenderer {
    [Serializable]
    public class HeroRendererAnimationEntry {
        [TemplateType(typeof(ItemTemplate))] public TemplateReference mainHandEquipment;
        [TemplateType(typeof(ItemTemplate))] public TemplateReference offHandEquipment;

        [BoxGroup("Animations")] public ClipTransition start;
        [BoxGroup("Animations")] public ClipTransition loop;

        public ItemTemplate MainHandEquipment => mainHandEquipment?.Get<ItemTemplate>();
        public ItemTemplate OffHandEquipment => offHandEquipment?.Get<ItemTemplate>();

        public bool Matches(ILoadout loadout) =>
            TemplateMatches(MainHandEquipment, loadout[EquipmentSlotType.MainHand]) &&
            TemplateMatches(OffHandEquipment, loadout[EquipmentSlotType.OffHand]);

        
        /// <summary>
        /// Simplified version only for Transmogrify, where we only care about some type of animations and weapons are in predefined slots.
        /// </summary>
        public bool MatchesForTransmog(Item item) {
            if (item.IsOneHanded && !item.IsShield) {
                return TemplateMatches(MainHandEquipment, item) && 
                       TemplateMatches(OffHandEquipment, World.Services.Get<CommonReferences>().DefaultOffHandFistsTemplate);
            }

            if (item.IsTwoHanded || item.IsRanged) {
                return TemplateMatches(MainHandEquipment, item) && 
                       TemplateMatches(OffHandEquipment, item);
            }

            if (item.IsShield) {
                return TemplateMatches(MainHandEquipment, World.Services.Get<CommonReferences>().DefaultMainHandFistsTemplate) && 
                       TemplateMatches(OffHandEquipment, item);
            }
            
            return false;
        }
        
        public bool MatchesForFists() {
            return TemplateMatches(MainHandEquipment, World.Services.Get<CommonReferences>().DefaultMainHandFistsTemplate) && 
                   TemplateMatches(OffHandEquipment, World.Services.Get<CommonReferences>().DefaultOffHandFistsTemplate);
        }
        
        bool TemplateMatches(ItemTemplate requiredEquipment, Item item) =>
            item != null && item.Template.InheritsFrom(requiredEquipment);
        
        bool TemplateMatches(ItemTemplate requiredEquipment, ItemTemplate itemTemplate) =>
            itemTemplate != null && itemTemplate.InheritsFrom(requiredEquipment);
    }
}