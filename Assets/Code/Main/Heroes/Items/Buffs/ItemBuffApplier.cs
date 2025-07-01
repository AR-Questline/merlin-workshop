using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using FMODUnity;

namespace Awaken.TG.Main.Heroes.Items.Buffs {
    public partial class ItemBuffApplier : Element<Item>, IRefreshedByAttachment<ItemBuffApplierAttachment>, ISkillOwner, ISkillProvider, IItemAction {
        public override ushort TypeForSerialization => SavedModels.ItemBuffApplier;

        ItemBuffApplierAttachment _spec;

        public float Duration => _spec.Duration(ItemLevel);
        public ShareableARAssetReference VFX => _spec.VFX;
        public IEnumerable<SkillReference> SkillRefs => _spec.Skills;
        public IEnumerable<Skill> Skills => Elements<Skill>().GetManagedEnumerator();
        // Character for ISkillOwner
        public ICharacter Character => null;
        public ItemActionType Type => ItemActionType.Use;
        int ItemLevel => ParentModel?.Level?.ModifiedInt ?? 0; 
        
        public void InitFromAttachment(ItemBuffApplierAttachment spec, bool isRestored) {
            _spec = spec;
            foreach (var skillRef in _spec.Skills) {
                // Skills are not saved (see AllowElementSave)
                var skill = skillRef.CreateSkill();
                AddElement(skill);
            }
        }

        protected override void OnFullyInitialized() {
            ParentModel.RequestSetupTexts();
        }

        public override bool AllowElementSave(Element ele) {
            return false;
        }

        // === IItemAction
        Item GetEquippedWeapon() {
            Item equipped = ParentModel.CharacterInventory?.EquippedItem(EquipmentSlotType.MainHand);
            if (equipped is { IsWeapon: true, IsFists: false, IsMelee: true }) {
                return equipped;
            }

            return null;
        }
        
        public void Submit() {
            Item equippedWeapon = GetEquippedWeapon();
            if (equippedWeapon == null) {
                //RuntimeManager.PlayOneShot(CommonReferences.Get.AudioConfig.StrongNegativeFeedbackSound);
                return;
            }
            
            AppliedItemBuff currentBuff = equippedWeapon.TryGetElement<AppliedItemBuff>();
            currentBuff?.Discard();
            
            AppliedItemBuff buff = new(this);
            foreach (var skillRef in SkillRefs) {
                Skill s = skillRef.CreateSkill();
                buff.AddElement(s);
            }
        
            equippedWeapon.AddElement(buff);

            if (ParentModel.Owner is Hero h) {
                h.Trigger(Hero.Events.ShowWeapons, true);
                h.Element<HeroOverridesFSM>().SetCurrentState(HeroStateType.WeaponBuff);
                World.Any<CharacterSheetUI>()?.Discard();
                World.Any<QuickUseWheelUI>()?.Discard();
            }
            
            ParentModel.DecrementQuantityWithoutNotification();
        }
        public void AfterPerformed() { }
        public void Perform() { }
        public void Cancel() { }
    }
}