using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility.PhysicUtils;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Heroes.Combat {
    [RequireComponent(typeof(VisualEffect))]
    public class VCToggleVFXOnCollisionWith : ViewComponent {
        [SerializeField] bool onlyInCombat = true;
        [SerializeField] VisualEffect visualEffect;
        [SerializeField] float radius = 0.25f;
        [SerializeField] LayerMask layerMask;

        bool _targetInCombat;
        bool _wasActive;
        
        protected override void OnAttach() {
            if (visualEffect == null) {
                visualEffect = GetComponent<VisualEffect>();
            }
            VFXUtils.StopVfx(visualEffect);
            GenericTarget.GetOrCreateTimeDependent().WithUpdate(OnUpdate);

            if (onlyInCombat && VGUtils.TryGetModel(gameObject, out NpcElement npc)) {
                npc.ListenTo(ICharacter.Events.CombatEntered, () => _targetInCombat = true, this);
                npc.ListenTo(ICharacter.Events.CombatExited, () => _targetInCombat = false, this);
            }
        }

        void OnUpdate(float deltaTime) {
            if (onlyInCombat && !_targetInCombat) {
                return;
            }
            
            bool collisionDetected = Physics.CheckSphere(transform.position, radius, layerMask, QueryTriggerInteraction.Ignore);

            if (!collisionDetected && _wasActive) {
                VFXUtils.StopVfx(visualEffect);
            } else if (collisionDetected && !_wasActive) {
                visualEffect.Play();
            }
            
            _wasActive = collisionDetected;
        }
        
        protected override void OnDiscard() {
            GenericTarget.GetTimeDependent().WithoutUpdate(OnUpdate);
            base.OnDiscard();
        }

        void OnDrawGizmosSelected() {
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}