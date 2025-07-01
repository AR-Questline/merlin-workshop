using System.Collections.Generic;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SpawnItemInHandWithDuration : ARUnit, ISkillUnit {
        const float DefaultManaCost = 5f;
        
        protected override void Definition() {
            var itemTemplate = RequiredARValueInput<TemplateWrapper<ItemTemplate>>("itemTemplate");
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            var duration = RequiredARValueInput<IDuration>("duration");
            var isRanged = RequiredARValueInput<bool>("isRanged");
            var magicProjectileRef = FallbackARValueInput("magicProjectile",
                _ => new TemplateWrapper<ItemTemplate>(World.Services.Get<GameConstants>().DefaultMagicArrowTemplate));
            var magicProjectileManaCost = FallbackARValueInput("magicProjectileManaCost", _ => DefaultManaCost);
            DefineSimpleAction(flow => {
                Item skillItem = this.Skill(flow).SourceItem;
                LockItemSlot itemSlotLocker = skillItem.AddElement<LockItemSlot>();
                
                ItemTemplate template = itemTemplate.Value(flow).Template;
                Item item = character.Value(flow).Inventory.Add(new Item(template));
                item.AddElement<LockItemSlot>();
                Hero.Current.Trigger(MagicHeavyLoop.Events.BeforeSpawnedNewItemInHand, item);

                Hero hero = Hero.Current;
                Dictionary<HeroLoadout, EquipmentSlotType> loadouts = new();
                List<HeroLoadoutSlotLocker> lockers = new();
                foreach (var loadout in hero.HeroItems.Loadouts) {
                    if (loadout.TryGetSlotOfItem(skillItem, out EquipmentSlotType slotType)) {
                        loadout.EquipItem(slotType, item);
                        loadouts[loadout] = slotType;
                        lockers.Add(loadout.AddElement(new HeroLoadoutSlotLocker(slotType)));
                    }
                }
                
                item.AddElement(new TemporaryItem(skillItem, itemSlotLocker, duration.Value(flow), loadouts, lockers));
                if (isRanged.Value(flow)) {
                    item.AddElement(new MagicRangedItem(magicProjectileRef.Value(flow).Template, magicProjectileManaCost.Value(flow), loadouts));
                }
            });
        }
    }
}