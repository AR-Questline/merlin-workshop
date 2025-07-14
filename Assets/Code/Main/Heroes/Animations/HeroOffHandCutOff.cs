using Awaken.Kandra.AnimationPostProcess;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Animations {
    public partial class HeroOffHandCutOff : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroOffHandCutOff;

        bool? _wasTppActive;
        
        protected override void OnInitialize() {
            Item item = ParentModel.HeroItems.Add(new Item(CommonReferences.Get.HandCutOffItemTemplate));
            item.AddElement<LockItemSlot>();
            foreach (var loadout in ParentModel.HeroItems.Loadouts) {
                loadout.EquipItem(EquipmentSlotType.OffHand, item);
                loadout.AddElement(new HeroLoadoutSlotLocker(EquipmentSlotType.OffHand));
            }
            ParentModel.OnVisualLoaded(ApplyAnimPP);
        }

        protected override void OnRestore() {
            ParentModel.OnVisualLoaded(ApplyAnimPP);
        }

        public void HeroPerspectiveChanged(bool tppActive) {
            if (_wasTppActive == tppActive) {
                return;
            }
            
            ApplyAnimPP();
        }

        void ApplyAnimPP() {
            _wasTppActive = Hero.TppActive;
            var animPP = ParentModel.VHeroController.BodyData.GetComponentInChildren<AnimationPostProcessing>();
            if (animPP != null) {
                animPP.ChangeAdditionalEntries(new[] { new AnimationPostProcessing.Entry(CommonReferences.Get.noLeftArmPP) });
            }
        }
    }
}