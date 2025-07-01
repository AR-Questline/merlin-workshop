using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Interactions {
    public partial class HeroToolAction : Element<Hero>, IUIPlayerInput {
        public sealed override bool IsNotSaved => true;

        ToolHeroActionSetting _heroActionSetting;

        public IEnumerable<KeyBindings> PlayerKeyBindings => KeyBindings.Gameplay.Interact.Yield();

        Tool ToolItem => ParentModel.Inventory.EquippedItem(EquipmentSlotType.MainHand)?.TryGetElement<Tool>()
                         ?? ParentModel.Inventory.EquippedItem(EquipmentSlotType.OffHand)?.TryGetElement<Tool>();

        public new class Events {
            public static readonly Event<Hero, bool> HeroToolInteracted = new(nameof(HeroToolInteracted));
        }
        
        protected override void OnInitialize() {
            World.Only<PlayerInput>().RegisterPlayerInput(this, this);
            _heroActionSetting = World.Only<ToolHeroActionSetting>();
        }

        // This callback will be sent last if no other Handlers consume the interaction
        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction && ToolItem is { } tool && _heroActionSetting.AllowHeroAction(tool.Type)) {
                StartToolAction(tool).Forget();
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }

        public async UniTaskVoid StartToolAction(Tool tool = null) {
            Hero current = Hero.Current;
            tool ??= ToolItem;
            var characterHandBase = tool.ParentModel.View<CharacterHandBase>();
            if (characterHandBase != null) {
                if (characterHandBase.IsHidden && tool.Type != ToolType.Spyglassing) {
                    characterHandBase.ShowWeapon();
                    if (!await AsyncUtil.DelayTime(current, 1))
                        return;
                }
            }
            
            current.Trigger(Events.HeroToolInteracted, true);
        }
    }
}