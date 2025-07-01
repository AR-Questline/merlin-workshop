using System;
using Animancer;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Templates;
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
        
        bool TemplateMatches(ItemTemplate requiredEquipment, Item item) =>
            item != null && item.Template.InheritsFrom(requiredEquipment);
    }
}