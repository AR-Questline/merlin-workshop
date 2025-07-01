using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    public partial class Barnaclator : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.Barnaclator;

        readonly NoMove _noMove = new();
        bool _combatActivated;
        HitboxDestroyable[] _hitboxes;
        
        public bool AnyHitboxLeft {
            get {
                foreach (var hitbox in _hitboxes) {
                    if (hitbox != null && !hitbox.Destroyed) {
                        return true;
                    }
                }
                return false;
            }
        }

        protected override void AfterVisualLoaded(Transform parentTransform) {
            base.AfterVisualLoaded(parentTransform);
            _hitboxes = parentTransform.GetComponentsInChildren<HitboxDestroyable>();
            NpcElement.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, ActivateCombat, this);
            NpcElement.ListenTo(NpcMovement.Events.OnMovementInterrupted, OnMovementInterrupted, this);
            NpcElement.OnVisualLoaded(AfterVisualFullyLoaded);
        }
        
        void AfterVisualFullyLoaded(NpcElement npc, Transform transform) {
            DisableCombat(true);
        }

        public Transform GetFirePoint(int index) {
            return _hitboxes[index] != null ? _hitboxes[index].transform : null;
        }

        protected override void Tick(float deltaTime, NpcElement npc) {
            if (!_combatActivated && CombatActivateConditionsMet()) {
                ActivateCombat();
            }
            
            if (!_combatActivated) {
                return;
            }
            
            base.Tick(deltaTime, npc);

            bool CombatActivateConditionsMet() => npc.NpcAI.HeroVisibility > 0.25f;
        }

        protected override void NotInCombatUpdate(float deltaTime) {
            if (!_combatActivated) {
                return;
            }
            
            base.NotInCombatUpdate(deltaTime);
        }

        protected override void OnExitCombat() {
            base.OnExitCombat();
            DisableCombat(false);
        }
        
        void ActivateCombat() {
            if (_combatActivated) {
                return;
            }
            _combatActivated = true;
            ParentModel.RemoveElementsOfType<HideEnemyFromPlayer>();
            SelectNewBehaviour();
        }
        
        void DisableCombat(bool forceChange) {
            if (!_combatActivated && !forceChange) {
                return;
            }
            _combatActivated = false;
            ParentModel.AddMarkerElement<HideEnemyFromPlayer>();
            NpcElement.Movement.InterruptState(_noMove);
        }

        void OnMovementInterrupted() {
            if (NpcElement.Movement.CurrentState != _noMove) {
                NpcElement.Movement.InterruptState(_noMove);
            }
        }
    }
}