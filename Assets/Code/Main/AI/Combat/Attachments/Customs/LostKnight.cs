using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [UnityEngine.Scripting.Preserve]
    public partial class LostKnight : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.LostKnight;

        bool _combatActivated;
        Animator _animator;
        AnimatorBridge _animatorBridge;

        bool OutOfHeroSight => !_animatorBridge.IsVisible;
        
        protected override void AfterVisualLoaded(Transform parentTransform) {
            base.AfterVisualLoaded(parentTransform);
            _animator = parentTransform.GetComponentInChildren<Animator>();
            _animatorBridge = AnimatorBridge.GetOrAddDefault(_animator);
            _animator.keepAnimatorStateOnDisable = true;
            
            NpcElement.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, ActivateCombat, this);
            NpcElement.OnVisualLoaded(AfterVisualFullyLoaded);
            
        }
        
        void AfterVisualFullyLoaded(NpcElement npc, Transform transform) {
            DisableCombat(true, false);
        }

        protected override void Tick(float deltaTime, NpcElement npc) {
            if (!_combatActivated && OutOfHeroSight && npc.IsTargetingHero()) {
                ActivateCombat();
            }
            
            if (!_combatActivated) {
                return;
            }
            
            base.Tick(deltaTime, npc);
        }

        protected override void NotInCombatUpdate(float deltaTime) {
            if (!_combatActivated) {
                return;
            }
            
            base.NotInCombatUpdate(deltaTime);
        }

        protected override void OnExitCombat() {
            base.OnExitCombat();
            DisableCombat(false, true);
        }

        void ActivateCombat() {
            if (_combatActivated) return;
            _combatActivated = true;
            _animator.enabled = true;
            ParentModel.RemoveElementsOfType<HideEnemyFromPlayer>();
            NpcElement.Movement.StopInterrupting();
            SelectNewBehaviour();
        }

        void DisableCombat(bool forceChange, bool disableAnimator) {
            if (!_combatActivated && !forceChange) return;
            _combatActivated = false;
            _animator.enabled = !disableAnimator;
            if (!ParentModel.HasElement<HideEnemyFromPlayer>()) {
                ParentModel.AddElement(new HideEnemyFromPlayer());
            }
            NpcElement.Movement.InterruptState(new NoMove());
        }
    }
}