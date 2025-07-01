using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class CharacterMagicGauntlet : CharacterWeapon {
        [FoldoutGroup("Collider Settings"), SerializeField] Transform offHandColliderOverride;
        
        protected override void OnAttachedToHero(Hero hero) {
            if (Item.EquippedInSlotOfType == EquipmentSlotType.OffHand && offHandColliderOverride != null) {
                colliderParent.localPosition = offHandColliderOverride.localPosition;
                colliderParent.localRotation = offHandColliderOverride.localRotation;
            }
            base.OnAttachedToHero(hero);
        }
        
        protected override void OnBoxCastHit(in RaycastHit other, bool inEnviroHitRange, in AttackParameters attackParameters) {
            HandOwner?.OnMagicGauntletTriggerEnter(other, attackParameters, inEnviroHitRange);
        }
    }

    public struct MagicGauntletData {
        [UnityEngine.Scripting.Preserve] public ICharacter attacker;
        [UnityEngine.Scripting.Preserve] public IAlive receiver;
        [UnityEngine.Scripting.Preserve] public Item item;
        [UnityEngine.Scripting.Preserve] public AttackParameters attackParameters;
        
        public MagicGauntletData(ICharacter attacker, IAlive receiver, Item item, AttackParameters attackParameters) {
            this.attacker = attacker;
            this.receiver = receiver;
            this.item = item;
            this.attackParameters = attackParameters;
        }
    }
}