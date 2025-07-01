using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;
using FMODUnity;

namespace Awaken.TG.Main.Heroes.Combat {
    public class CharacterMagic : CharacterHandBase {
        static readonly int ChargeIncreased = Animator.StringToHash("ChargeIncreased");
        static readonly int ChargesSpend = Animator.StringToHash("ChargesSpend");
        static readonly int FailedCast = Animator.StringToHash("FailedCast");
        
        [SerializeField] Animator equipAbleAnimator;
        [FoldoutGroup("MainHand Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference animatorControllerRefMainHand;
        [FoldoutGroup("MainHand Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference animatorControllerRefMainHandTPP;
        [FoldoutGroup("MainHand Settings"), ARAssetReferenceSettings(new []{typeof(GameObject)}, true, AddressableGroup.Weapons)]
        public ARAssetReference mainHandMagicGlove;
        [FoldoutGroup("MainHand Settings"), ValueDropdown(nameof(Layers))]
        public string[] layersToEnableMainHand = Array.Empty<string>();
        [FoldoutGroup("OffHand Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference animatorControllerRefOffHand;
        [FoldoutGroup("OffHand Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference animatorControllerRefOffHandTPP;
        [FoldoutGroup("OffHand Settings"), ARAssetReferenceSettings(new []{typeof(GameObject)}, true, AddressableGroup.Weapons)]
        public ARAssetReference offHandMagicGlove;
        [FoldoutGroup("OffHand Settings"), ValueDropdown(nameof(Layers))]
        public string[] layersToEnableOffHand = Array.Empty<string>();
        
        [FoldoutGroup("VFX Settings"), SerializeField] bool ignoreVisualFirePoint;
        [FoldoutGroup("VFX Settings"), SerializeField] Transform customVisualFirePoint;
        [FoldoutGroup("VFX Settings"), SerializeField, GradientUsage(true)] Gradient magicGauntletGradient;
        [FoldoutGroup("VFX Settings"), SerializeField, ColorUsage(true, true)] Color magicGauntletColor;
        [FoldoutGroup("VFX Settings"), SerializeField] bool useHighGlowOnCharge;
        [FoldoutGroup("VFX Settings"), SerializeField] bool noGlowOnRelease;
        [FoldoutGroup("VFX Settings"), SerializeField] float defaultGlow = 0.6f;
        [FoldoutGroup("VFX Settings"), SerializeField] float lowGlow = 1f;
        [FoldoutGroup("VFX Settings"), SerializeField] float highGlow = 1.4f;

        ARAssetReference _mainHandMagicGauntlet;
        ARAssetReference _offHandMagicGauntlet;
        IEventListener _heroDiedListener;
        IEventListener _heroReviveListener;
        VCCharacterMagicVFX[] _customClothesVFXs = Array.Empty<VCCharacterMagicVFX>();
        ARFmodEventEmitter _idleAudioEmitter;
        
        public override Transform VisualFirePoint => ignoreVisualFirePoint ? null : (customVisualFirePoint != null ? customVisualFirePoint : transform);
        public CastingHand CastingHand => _attachedToOffHand ? CastingHand.OffHand : CastingHand.MainHand;
        ARAssetReference MainHandAnimatorControllerRef => Hero.TppActive ? animatorControllerRefMainHandTPP : animatorControllerRefMainHand;
        ARAssetReference OffHandAnimatorControllerRef => Hero.TppActive ? animatorControllerRefOffHandTPP : animatorControllerRefOffHand;
        
        bool _attachedToOffHand;

        protected override void OnInitialize() {
            base.OnInitialize();
            if (Owner?.Character != null) {
                AttachWeaponEventsListener();
            }

            AttachIdleAudioEmitter();
        }
        
        protected override void OnAttachedToHero(Hero hero) {
            _attachedToOffHand = transform.parent == hero.OffHand;
            
            EquipMagicGloveToHero(hero, !_attachedToOffHand).Forget();
            if (Item is { IsTwoHanded: true }) {
                EquipMagicGloveToHero(hero, _attachedToOffHand).Forget();
            }

            _heroDiedListener = ModelUtils.ListenToFirstModelOfType(Hero.Events.Died, () => OnDied().Forget(), this);
            _heroReviveListener = ModelUtils.ListenToFirstModelOfType(Hero.Events.Revived, OnRevived, this);
            LoadHeroAnimatorOverrides();
        }

        protected override void LoadHeroAnimatorOverrides() {
            LoadAnimatorController(_attachedToOffHand ? OffHandAnimatorControllerRef : MainHandAnimatorControllerRef).Forget();
        }

        protected override void OnAttachedToNpc(NpcElement npcElement) {
            _attachedToOffHand = transform.parent == npcElement.OffHand;
        }
        
        protected override void OnAttachedToCustomHeroClothes(CustomHeroClothes clothes, ItemEquip equip) {
            _attachedToOffHand = transform.parent == clothes.OffHandSocket;
            
            EquipMagicGloveToCustomHeroClothes(clothes, equip, !_attachedToOffHand).Forget();
            if (Item is { IsTwoHanded: true }) {
                EquipMagicGloveToCustomHeroClothes(clothes, equip, _attachedToOffHand).Forget();
            }

            _customClothesVFXs = GetComponentsInChildren<VCCharacterMagicVFX>(true);
            foreach (VCCharacterMagicVFX vfx in _customClothesVFXs) {
                AttachCharacterMagicVFXForCustomClothes(clothes, vfx, equip);
            }
            
            base.OnAttachedToCustomHeroClothes(clothes, equip);
        }

        protected override void OnDetachedFromCustomHeroClothes(CustomHeroClothes clothes) {
            foreach (var vfx in _customClothesVFXs) {
                if (vfx != null) {
                    Destroy(vfx.gameObject);
                }
            }
            _customClothesVFXs = Array.Empty<VCCharacterMagicVFX>();
            RemoveAllMagicGauntletsFrom(clothes);
        }

        protected override void ToggleAnimatorLayers(bool activate) {
            if (Owner is not Hero hero) {
                return;
            }
            
            string[] layersToEnable = _attachedToOffHand ? layersToEnableOffHand : layersToEnableMainHand;
            var states = hero.Elements<HeroAnimatorSubstateMachine>().Where(fsm => {
                return layersToEnable.Any(l => l.Equals(fsm.ParentLayerName, StringComparison.InvariantCultureIgnoreCase));
            });
            
            foreach (HeroAnimatorSubstateMachine fsm in states) {
                if (activate) {
                    fsm.EnableFSM();
                } else {
                    fsm.DisableFSM();
                }
            }
        }

        async UniTaskVoid EquipMagicGloveToHero(Hero hero, bool mainHand) {
            var clothes = hero.HandClothes;

            var glove = await PrepareMagicGlove(clothes, mainHand);
            
            if (HasBeenDiscarded) {
                RemoveAllMagicGauntletsFrom(clothes);
                return;
            }
            SetupMagicGauntlet(glove, Item);
        }

        async UniTaskVoid EquipMagicGloveToCustomHeroClothes(CustomHeroClothes clothes, ItemEquip equip, bool mainHand) {
            var glove = await PrepareMagicGlove(clothes, mainHand);

            if (!this || !gameObject || !gameObject.activeSelf) {
                RemoveAllMagicGauntletsFrom(clothes);
                return;
            }
            SetupMagicGauntlet(glove, equip.Item);
        }
        
        async UniTask<GameObject> PrepareMagicGlove(IBaseClothes clothes, bool mainHand) {
            if (mainHand && _mainHandMagicGauntlet != null) {
                DestroyMagicGauntlet(true);
            }

            if (!mainHand && _offHandMagicGauntlet != null) {
                DestroyMagicGauntlet(false);
            }

            var gloveReference = mainHand ? mainHandMagicGlove : offHandMagicGlove;
            if (gloveReference is not { IsSet: true }) {
                return null;
            }
            
            if (mainHand) {
                _mainHandMagicGauntlet = gloveReference;
            } else {
                _offHandMagicGauntlet = gloveReference;
            }
            
            (GameObject glove, bool isSuccess) = await clothes.EquipTask(gloveReference, BaseClothes.ShadowsOverride.ForceOff);
            if (!isSuccess) {
                clothes.Unequip(gloveReference);
                return null;
            }
            
            return glove;
        }

        void SetupMagicGauntlet(GameObject glove, Item owningItem) {
            if (glove == null) {
                return;
            }
            // Since magic gauntlet is equipped by ClothStitcher all components are removed from original prefab.
            // That's why we are adding this component here.
            VCMagicGauntlet magicGauntlet = glove.AddComponent<VCMagicGauntlet>();
            magicGauntlet.Init(magicGauntletGradient, magicGauntletColor, useHighGlowOnCharge, noGlowOnRelease, defaultGlow, lowGlow, highGlow);
            magicGauntlet.Attach(Services, owningItem, this);
        }

        protected override void OnWeaponHidden() {
            base.OnWeaponHidden();

            if (Owner is Hero hero) {
                RemoveAllMagicGauntletsFrom(hero.HandClothes);
                
                World.EventSystem.TryDisposeListener(ref _heroReviveListener);
                World.EventSystem.TryDisposeListener(ref _heroDiedListener);
            }
        }

        void DestroyMagicGauntlet(bool mainHand) {
            if (Owner is Hero h) {
                if (mainHand && _mainHandMagicGauntlet != null) {
                    h.HandClothes.Unequip(_mainHandMagicGauntlet);
                    _mainHandMagicGauntlet = null;
                }

                if (!mainHand && _offHandMagicGauntlet != null) {
                    h.HandClothes.Unequip(_offHandMagicGauntlet);
                    _offHandMagicGauntlet = null;
                }
            }
        }

        void RemoveAllMagicGauntletsFrom(IBaseClothes clothes) {
            if (_mainHandMagicGauntlet != null) {
                clothes.Unequip(_mainHandMagicGauntlet);
                _mainHandMagicGauntlet = null;
            }

            if (_offHandMagicGauntlet != null) {
                clothes.Unequip(_offHandMagicGauntlet);
                _offHandMagicGauntlet = null;
            }
        }
        
        void OnRevived() {
            transform.localScale = Vector3.one;        
        }

        async UniTaskVoid OnDied() {
            await DOTween.To(() => transform.localScale, x => transform.localScale = x, Vector3.zero, 0.15f);
        }

        void AttachCharacterMagicVFXForCustomClothes(CustomHeroClothes customClothes, VCCharacterMagicVFX vfx, ItemEquip equip) {
            var vfxTransform = vfx.transform;

            Vector3 originalPosition = vfxTransform.localPosition;
            Quaternion originalRotation = vfxTransform.localRotation;
            if (vfx.VfxParent is VFXParent.CharacterBase or VFXParent.CharacterArms) {
                if (vfx.VfxParent == VFXParent.CharacterArms) {
                    bool equippedInMainHand = equip.Item.EquippedInSlotOfType == EquipmentSlotType.MainHand;
                    vfx.transform.SetParent(equippedInMainHand ? customClothes.MainHandSocket : customClothes.OffHandSocket);
                } else {
                    vfx.transform.SetParent(customClothes.RootSocket);
                }
            } else if (vfx.VfxParent is VFXParent.CharacterWrist) {
                bool equippedInMainHand = equip.Item.EquippedInSlotOfType == EquipmentSlotType.MainHand;
                vfx.transform.SetParent(equippedInMainHand ? customClothes.MainHandWristSocket : customClothes.OffHandWristSocket);
            }

            vfxTransform.localPosition = originalPosition;
            vfxTransform.localRotation = originalRotation;
            vfxTransform.localScale = Vector3.one;
        }
        
        void AttachIdleAudioEmitter() {
            _idleAudioEmitter = gameObject.AddComponent<ARFmodEventEmitter>();
            // _idleAudioEmitter.EventPlayTrigger = EmitterGameEvent.ObjectEnable;
            // _idleAudioEmitter.EventStopTrigger = EmitterGameEvent.ObjectDisable;
            PlayIdleHeldAudio();
            
            ICharacter characterOwner = Owner as ICharacter;
            characterOwner?.ListenTo(ICharacter.Events.CastingBegun, PlayIdleHeldChargedAudio, this);
            characterOwner?.ListenTo(ICharacter.Events.CastingCanceled, PlayIdleHeldAudio, this);
            characterOwner?.ListenTo(ICharacter.Events.CastingEnded, PlayIdleHeldAudio, this);
        }

        void PlayIdleHeldAudio() {
            PlayIdleAudioEvent(ItemAudioType.MagicHeldIdle.RetrieveFrom(Item));
        }

        void PlayIdleHeldChargedAudio() {
            PlayIdleAudioEvent(ItemAudioType.MagicHeldChargedIdle.RetrieveFrom(Item));
        }

        void PlayIdleAudioEvent(EventReference eventRef) {
            //_idleAudioEmitter.ChangeEvent(eventRef, false);
            // if (gameObject.activeInHierarchy) {
            //     _idleAudioEmitter.Play();
            // }
        }

        // === Public API for VS
        public void OnMagicSuccessfullyStarted() {
            Target.Trigger(VCCharacterMagicVFX.Events.OnMagicSuccessfullyStarted, Target);
        }
        
        public void OnSoulCubeChargeIncreased() {
            if (equipAbleAnimator != null) {
                equipAbleAnimator.SetTrigger(ChargeIncreased);
            }
            
            Target.Trigger(VCCharacterMagicVFX.Events.SoulCubeChargeIncreased, Target);
        }
        
        public void OnSoulCubeChargesSpend() {
            if (equipAbleAnimator != null) {
                equipAbleAnimator.SetTrigger(ChargesSpend);
            }
            
            Target.Trigger(VCCharacterMagicVFX.Events.SoulCubeChargesSpend, Target);
        }

        public void OnFailedCast() {
            if (equipAbleAnimator != null) {
                equipAbleAnimator.SetTrigger(FailedCast);
            }
        }
        
        public void OnMagicSuccessfullyEnded() {
            Target.Trigger(VCCharacterMagicVFX.Events.OnMagicSuccessfullyEnded, Target);
        }
    }
}