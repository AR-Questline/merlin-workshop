using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroHorseArmorHandler : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroHorseArmorHandler;

        [Saved] bool _usingHorseArmorDlc = false;

        IEventListener _stateStackListener;
        
        Hero Hero => ParentModel;
        public bool SlotHidden => !_usingHorseArmorDlc || !Hero.OwnedMount.IsSet;
        public bool SlotLocked => !_usingHorseArmorDlc || !Hero.OwnedMount.TryGet(out var mount) || !mount.CanUseArmor;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            Hero.ListenTo(MountElement.Events.HeroMounted, OnHeroMounted, this);
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            CheckDlcStatus();
        }

        void OnHeroMounted(MountElement mount) {
            TryShowTutorial();

            if (!mount.CanUseArmor) {
                Hero.HeroItems.Unequip(EquipmentSlotType.HorseArmor);
            }
        }

        void TryShowTutorial() {
            if (SlotLocked || TutorialKeys.IsConsumed(TutKeys.TriggerHorseArmorDlc)) {
                return;
            }

            var stateStack = UIStateStack.Instance;
            if (!TryShowTutorialWithState(stateStack.State)) {
                _stateStackListener = stateStack.ListenTo(UIStateStack.Events.UIStateChanged, state => TryShowTutorialWithState(state), this);
            }
        }

        bool TryShowTutorialWithState(UIState state) {
            if (!state.IsMapInteractive) {
                return false;
            }

            TutorialMaster.Trigger(TutKeys.TriggerHorseArmorDlc);
            World.EventSystem.TryDisposeListener(ref _stateStackListener);
            return true;
        }

        void CheckDlcStatus() {
            var newStatus = IsDlcInstalled();

            if (newStatus != _usingHorseArmorDlc) {
                _usingHorseArmorDlc = newStatus;
                OnDlcStatusChanged();
            }
        }
        
        void OnDlcStatusChanged() {
            if (_usingHorseArmorDlc) {
                var horseArmor = CommonReferences.Get.HorseDlcItem.ToRuntimeData(Hero);
                if (!Hero.HeroItems.HasItem(horseArmor)) {
                    Hero.HeroItems.AddWithoutNotification(new Item(horseArmor));
                }
                TryShowTutorial();
            } else {
                Hero.HeroItems.Unequip(EquipmentSlotType.HorseArmor);
            }
        }
        
        bool IsDlcInstalled() {
            var dlcIds = CommonReferences.Get.HorseDlcIds;
            return dlcIds.Any(SocialService.Get.HasDlc);
        }
    }
}