using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Heroes.Combat {
    public abstract class VCCharacterMagicVFX : ViewComponent<Item> {
        [SerializeField] VFXParent vfxParent;
        [SerializeField, Range(0f, 10f)] protected float enableVFXDelay = 0;
        [SerializeField, Range(0f, 10f)] protected float disableVFXDelay = 0;
        protected VisualEffect _visualEffect;
        CastingHand _castingHand;
        
        // === Properties
        public Item Item => Target;
        public IItemOwner Owner => Item?.Owner;
        public VFXParent VfxParent => vfxParent;
        
        // === Events
        public static class Events {
            public static readonly Event<Item, Item> OnMagicSuccessfullyStarted = new(nameof(OnMagicSuccessfullyStarted));
            public static readonly Event<Item, Item> OnMagicSuccessfullyEnded = new(nameof(OnMagicSuccessfullyEnded));
            public static readonly Event<Item, MagicVFXParam> ChangeMagicVFXParam = new(nameof(ChangeMagicVFXParam));
            public static readonly Event<Item, Transform> VFXTargetChanged = new(nameof(VFXTargetChanged));
            public static readonly Event<Item, Item> SoulCubeChargeIncreased = new(nameof(SoulCubeChargeIncreased));
            public static readonly Event<Item, Item> SoulCubeChargesSpend = new(nameof(SoulCubeChargesSpend));
        }

        protected override void OnAttach() {
            AsyncOnAttach().Forget();
        }

        protected async UniTaskVoid AsyncOnAttach() {
            if (GenericTarget is not Items.Item) {
                Log.Important?.Error($"{nameof(VCCharacterMagicVFX)} attached to Model that is not Item! This is invalid! Attached to: {GenericTarget}, This: {this}", gameObject);
                Destroy(gameObject, 0.1f);
                return;
            }
            
            bool success = await AsyncUtil.WaitWhile(gameObject, () => Item.View<CharacterMagic>() == null);
            if (!success || (Target?.HasBeenDiscarded ?? true)) {
                return;
            }

            Item.ListenTo(Item.Events.Unequipped, _ => {
                if (!this) return;
                if (!gameObject.activeSelf) {
                    World.EventSystem.RemoveAllListenersOwnedBy(this, true);
                }
                Destroy(gameObject);
            }, this);
            
            // We need to make sure that gameObject was enabled at least once so that we will get OnDestroy callback.
            EnsureOnEnableEvent();
            Initialize();
        }

        protected virtual void EnsureOnEnableEvent() {
            if (!gameObject.activeSelf) {
                gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        protected virtual void Initialize() {
            _visualEffect = GetComponentInChildren<VisualEffect>();
            _castingHand = Item.View<CharacterMagic>().CastingHand;
            AttachVFX();
            AttachListeners();
        }

        protected virtual void AttachVFX() {
            if (Owner == null) {
                return;
            }
            
            Vector3 originalPosition = transform.localPosition;
            Quaternion originalRotation = transform.localRotation;
            if (vfxParent is VFXParent.CharacterBase or VFXParent.CharacterArms) {
                if (vfxParent == VFXParent.CharacterArms && Owner is Hero h) {
                    transform.SetParent(h.VHeroController.fppParent.transform);
                } else {
                    transform.SetParent(Owner.MainView.transform);
                }
            } else if (vfxParent is VFXParent.CharacterWrist && Owner is Hero h) {
                bool equippedInMainHand = Item.EquippedInSlotOfType == EquipmentSlotType.MainHand;
                transform.SetParent(equippedInMainHand ? h.VHeroController.MainHandWrist : h.VHeroController.OffHandWrist);
            }
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = Vector3.one;
        }
        
        void AttachListeners() {
            ICharacter characterOwner = Owner as ICharacter;
            characterOwner?.ListenTo(ICharacter.Events.CastingBegun, CastingBegun, this);
            characterOwner?.ListenTo(ICharacter.Events.CastingCanceled, CastingCanceled, this);
            characterOwner?.ListenTo(ICharacter.Events.CastingFailed, CastingFailed, this);
            characterOwner?.ListenTo(ICharacter.Events.CastingEnded, CastingEnded, this);
            Target.ListenTo(Events.OnMagicSuccessfullyStarted, OnCastingSuccessfullyBegun, this);
            Target.ListenTo(Events.OnMagicSuccessfullyEnded, OnCastingSuccessfullyEnded, this);
            Target.ListenTo(Events.ChangeMagicVFXParam, ChangeVFXParam, this);
            Target.ListenTo(Events.SoulCubeChargeIncreased, SoulCubeChargeIncreased, this);
            Target.ListenTo(Events.SoulCubeChargesSpend, SoulCubeChargesSpend, this);
        }

        void CastingBegun(CastSpellData data) {
            if (_castingHand != data.CastingHand && data.CastingHand != CastingHand.BothHands) return;
            OnCastingBegun();
            if (_visualEffect != null) {
                _visualEffect.SendEvent("Begun");
            }
        }
        
        void CastingCanceled(CastSpellData data) {
            if (_castingHand != data.CastingHand && data.CastingHand != CastingHand.BothHands) return;
            OnCastingCanceled();
            if (_visualEffect != null) {
                _visualEffect.SendEvent("Canceled");
            }
        }
        
        void CastingFailed(CastSpellData data) {
            if (_castingHand != data.CastingHand && data.CastingHand != CastingHand.BothHands) return;
            OnCastingFailed();
            if (_visualEffect != null) {
                _visualEffect.SendEvent("Failed");
            }
        }

        void CastingEnded(CastSpellData data) {
            if (_castingHand != data.CastingHand && data.CastingHand != CastingHand.BothHands) return;
            OnCastingEnded();
            if (_visualEffect != null) {
                _visualEffect.SendEvent("Ended");
            }
        }

        void ChangeVFXParam(MagicVFXParam vfxParam) {
            if (_visualEffect != null) {
                vfxParam.SetVisualEffectParam(_visualEffect);
            }
        }
        
        protected virtual void OnCastingBegun() {}
        protected virtual void OnCastingSuccessfullyBegun() {}
        protected virtual void OnCastingFailed() {}
        protected virtual void OnCastingCanceled() {}
        protected virtual void OnCastingEnded() {}
        protected virtual void OnCastingSuccessfullyEnded() {}
        protected virtual void SoulCubeChargeIncreased(Item item) { }
        protected virtual void SoulCubeChargesSpend(Item item) { }
    }

    public enum VFXParent {
        [UnityEngine.Scripting.Preserve] CharacterBase = 0,
        [UnityEngine.Scripting.Preserve] CharacterHand = 1,
        [UnityEngine.Scripting.Preserve] CharacterArms = 2,
        [UnityEngine.Scripting.Preserve] CharacterWrist = 3,
    }
}
