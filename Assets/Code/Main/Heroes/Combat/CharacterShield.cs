using Awaken.TG.Assets;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Animations;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class CharacterShield : CharacterWeapon {
        // === Fields
        [FoldoutGroup("Hero Settings"), HeroAnimancerAnimationsAssetReference, PropertyOrder(-1)]
        public ARAssetReference animationsWhenSoloWieldingInMainHand, animationsWhenSoloWieldingInMainHandTpp;
        [FoldoutGroup("Hero Settings"), ValueDropdown(nameof(Layers))]
        public string[] layersToEnableWhenInMainHand = Array.Empty<string>();
        
        // TODO: copy data from previous system
        [SerializeField] HitboxData hitboxData;
        [SerializeField] Collider hitboxCollider;

        // === Properties
        protected override string[] LayersToEnable => _equippedInMainHand ? layersToEnableWhenInMainHand : layersToEnable;
        protected override ARAssetReference AnimatorControllerRef {
            get {
                if (Hero.TppActive) {
                    return _equippedInMainHand ? animationsWhenSoloWieldingInMainHandTpp : animatorControllerRefTpp;
                }
                
                return _equippedInMainHand ? animationsWhenSoloWieldingInMainHand : animatorControllerRef;
            }
        }

        // === Lifecycle
        protected override void OnAttachedToNpc(NpcElement npc) {
            base.OnAttachedToNpc(npc);
            
            if (hitboxCollider) {
                hitboxCollider.gameObject.SetActive(true);
                hitboxCollider.enabled = true;
                npc.HealthElement.AddHitbox(hitboxCollider, hitboxData);
            }
        }

        protected override IBackgroundTask OnDiscard() {
            if (hitboxCollider && Owner?.Character is NpcElement npcElement) {
                npcElement.HealthElement.RemoveHitbox(hitboxCollider);
            }
            
            return base.OnDiscard();
        }
    }
}
