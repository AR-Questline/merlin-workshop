using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public abstract class CharacterHand : CharacterHandBase {
        // === Hero
        [FoldoutGroup("Hero Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference animatorControllerRef, animatorControllerRefTpp;
        [Space]
        [FoldoutGroup("Hero Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference interactionAnimationOverride, interactionAnimationOverrideTpp;
        [Space]
        [FoldoutGroup("Hero Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference dualWieldingMainHand, dualWieldingMainHandTpp;
        [Space]
        [FoldoutGroup("Hero Settings"), HeroAnimancerAnimationsAssetReference]
        public ARAssetReference dualWieldingOffHand, dualWieldingOffHandTpp;
        [Space]
        [FoldoutGroup("Hero Settings"), ValueDropdown(nameof(Layers))]
        public string[] layersToEnable = Array.Empty<string>();
        
        protected virtual ARAssetReference AnimatorControllerRef => Hero.TppActive ? animatorControllerRefTpp : animatorControllerRef;
        protected ARAssetReference InteractionAnimationOverride => Hero.TppActive ? interactionAnimationOverrideTpp : interactionAnimationOverride;
        protected virtual string[] LayersToEnable => layersToEnable;

        ARAssetReference _cachedDualWieldingMainHand;
        ARAssetReference _cachedDualWieldingMainHandTpp;
        ARAssetReference DualWieldingMainHand {
            get {
                if (Hero.TppActive) {
                    if (dualWieldingMainHandTpp?.IsSet ?? false) {
                        return dualWieldingMainHandTpp;
                    }
                    return _cachedDualWieldingMainHandTpp ??= Services.Get<GameConstants>().defaultDualWieldingMainHandTpp.Get();
                }
                
                if (dualWieldingMainHand?.IsSet ?? false) {
                    return dualWieldingMainHand;
                }
                return _cachedDualWieldingMainHand ??= Services.Get<GameConstants>().defaultDualWieldingMainHand.Get();
            }
        }
        
        ARAssetReference _cachedDualWieldingOffHand;
        ARAssetReference _cachedDualWieldingOffHandTpp;
        ARAssetReference DualWieldingOffHand {
            get {
                if (Hero.TppActive) {
                    if (dualWieldingOffHandTpp?.IsSet ?? false) {
                        return dualWieldingOffHandTpp;
                    }
                    return _cachedDualWieldingOffHandTpp ??= Services.Get<GameConstants>().defaultDualWieldingOffHandTpp.Get();
                }
                
                if (dualWieldingOffHand?.IsSet ?? false) {
                    return dualWieldingOffHand;
                }
                return _cachedDualWieldingOffHand ??= Services.Get<GameConstants>().defaultDualWieldingOffHand.Get();
            }
        }
        
        protected override void OnAttachedToHero(Hero hero) {
            LoadHeroAnimatorOverrides();
        }

        protected override void LoadHeroAnimatorOverrides() {
            bool isInMainHand = Item.EquippedInSlotOfType == EquipmentSlotType.MainHand;
            ARAssetReference dualWieldingAsset = isInMainHand ? DualWieldingMainHand : DualWieldingOffHand;
            LoadAnimatorController(AnimatorControllerRef, InteractionAnimationOverride, dualWieldingAsset).Forget();
        }
        
        protected override void ToggleAnimatorLayers(bool activate) {
            if (Owner is not Hero hero) {
                return;
            }

            if (gameObject == null) {
                return;
            }

            if (hero.VHeroController.BodyData == null) {
                return;
            }

            DualHandedFSM dualHandedFSM = hero.Element<DualHandedFSM>();
            if (hero.IsDualWielding && activate) {
                if (HeroWeaponEvents.Current.IsLoadingAnimations()) {
                    return;
                }
                
                dualHandedFSM.EnableFSM();
                return;
            }

            if (!activate) {
                dualHandedFSM.DisableFSM();
            }

            var states = hero.Elements<HeroAnimatorSubstateMachine>().Where(fsm => {
                return LayersToEnable.Any(l => l.Equals(fsm.ParentLayerName, StringComparison.InvariantCultureIgnoreCase));
            });
                
            foreach (HeroAnimatorSubstateMachine fsm in states) {
                if (activate) {
                    fsm.EnableFSM();
                } else {
                    fsm.DisableFSM();
                }
            }
        }
    }
}