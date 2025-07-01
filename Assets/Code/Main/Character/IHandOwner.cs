using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Character {
    public interface IHandOwner<out T> : IElement<T> where T : ICharacter {
        float WeaponColliderDivider { get; }
        LayerMask HitLayerMask { get; } 
        void OnFinisherRelease(Vector3 weaponPosition);
        void OnBackStabRelease();

        public void OnAttackRelease(ARAnimationEventData eventData) {
            ParentModel.Trigger(ICharacter.Events.OnAttackRelease, eventData);
        }
        public void OnAttackRecovery(ARAnimationEventData eventData) {
            ParentModel.Trigger(ICharacter.Events.OnAttackRecovery, eventData);
        }
        
        public void OnQuickUseItemUsed(ARAnimationEventData eventData) {
            ParentModel.Trigger(ICharacter.Events.OnQuickUseItemUsed, eventData);
        }

        public void OnEffectInvoke(ARAnimationEventData arAnimationEvent) {
            ParentModel.Trigger(ICharacter.Events.OnEffectInvokedAnimationEvent, arAnimationEvent);
        }
        
        void OnWeaponTriggerEnter(RaycastHit hit, in AttackParameters attackParameters, bool inEnviroHitRange) {
            ParentModel?.CharacterDealingDamage?.OnWeaponTriggerEnter(hit, attackParameters, inEnviroHitRange, true);
        }

        void OnMagicGauntletTriggerEnter(RaycastHit hit, in AttackParameters attackParameters, bool inEnviroHitRange) {
            ParentModel?.CharacterDealingDamage?.OnMagicGauntletTriggerEnter(hit, attackParameters, inEnviroHitRange, true);
        }
        
        public void OnAttackEnded(AttackParameters attackParameters) {
            ParentModel.Trigger(ICharacter.Events.OnAttackEnd, attackParameters);
            if (ParentModel.CharacterDealingDamage.AnyTargetHit) {
                ParentModel.Trigger(ICharacter.Events.OnSuccessfulAttackEnd, attackParameters);
            } else {
                ParentModel.Trigger(ICharacter.Events.OnFailedAttackEnd, attackParameters);
            }
        }
    }
}
