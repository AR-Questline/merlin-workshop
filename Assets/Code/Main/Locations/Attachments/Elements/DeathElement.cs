using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.CustomDeath;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class DeathElement : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        bool IsDead { [UnityEngine.Scripting.Preserve] get; set; }
        bool _keepBody;
        List<IDeathBehaviour> _behaviours;
        
        public bool KeepBody => _keepBody;
        public bool CanUseExternalCustomDeath { get; private set; } = true;
        Location Location => ParentModel.ParentModel;
        VDynamicLocation View => Location.View<VDynamicLocation>();
        
        // === Events
        public new static class Events {
            public static readonly Event<ICharacter, bool> RagdollToggled = new(nameof(RagdollToggled));
            public static readonly Event<Location, bool> RefreshDeathBehaviours = new(nameof(RefreshDeathBehaviours));
        }

        protected override void OnInitialize() {
            Location.OnVisualLoaded(Init);
        }

        void Init(Transform parentTransform) {
            RefreshBehaviours(parentTransform);
            
            Location.ListenTo(Events.RefreshDeathBehaviours, _ => {
                RefreshBehaviours(parentTransform);
            }, this);
        }
        
        void RefreshBehaviours(Transform parentTransform) {
            CreateBehaviours(parentTransform);
            CanUseExternalCustomDeath = true;
            foreach (var behaviour in _behaviours) {
                if (!behaviour.IsVisualInitialized) {
                    behaviour.OnVisualLoaded(this, parentTransform);
                }
                CanUseExternalCustomDeath = CanUseExternalCustomDeath && !behaviour.BlockExternalCustomDeath;
            }
        }

        void CreateBehaviours(Transform parentTransform) {
            foreach (var deathBehaviourUpdater in Location.Elements<DeathBehaviourUpdater>()) {
                deathBehaviourUpdater.UpdateDeathBehaviours(parentTransform.gameObject);
            }
            
            CustomDeathController custom = parentTransform.GetComponentInParent<LocationParent>(true)?.GetComponentInChildren<CustomDeathController>(true);

            var ragdollBehaviour = CreateDeathRagdollNpcBehaviour(custom);
            _behaviours = new List<IDeathBehaviour>();
            if (custom == null) {
                _keepBody = true;
                _behaviours.Add(ragdollBehaviour);
            } else {
                _keepBody = custom.keepBody;
                if (custom.AddRagdollBehaviour) {
                    _behaviours.Add(ragdollBehaviour);
                }
                foreach (var additional in custom.GetAdditionalBehaviours()) {
                    _behaviours.Add(additional);
                }
            }
        }

        DeathRagdollNpcBehaviour CreateDeathRagdollNpcBehaviour(CustomDeathController custom) {
            DeathRagdollNpcBehaviour oldRagdoll = _behaviours?.FirstOrDefault(b => b is DeathRagdollNpcBehaviour) as DeathRagdollNpcBehaviour;
            bool canRagdollWhenAlive = custom?.CanRagdollWhenAlive ?? true;
            bool shouldAlwaysRagdollOnDeath = custom?.ShouldRagdollOnDeath ?? true;
            // We need to copy data from ragdoll behaviour if it existed because once cached ragdoll can't be cached again
            return oldRagdoll != null ? new DeathRagdollNpcBehaviour(oldRagdoll, canRagdollWhenAlive, shouldAlwaysRagdollOnDeath) : new DeathRagdollNpcBehaviour(canRagdollWhenAlive, shouldAlwaysRagdollOnDeath);
        }

        public T GetBehaviour<T>() => _behaviours.OfType<T>().FirstOrDefault();
        
        public void OnDeath(DamageOutcome damageOutcome) {
            IsDead = true;
            
            // --- Change hitboxes to triggers if body is not destroyed
            if (_keepBody) {
                IEnumerable<Collider> colliders = View.ModelInstance.GetComponentsInChildren<Collider>()
                    .Where(c => c.gameObject.layer == RenderLayers.Hitboxes);
                foreach (var collider in colliders.ToList()) {
                    collider.isTrigger = true;
                }
            }
            
            bool useDeathAnim = false;
            NpcDeath.DeathAnimType deathAnimType = NpcDeath.DeathAnimType.Default;
            var location = ParentModel?.ParentModel;
            foreach (var behaviour in _behaviours) {
                behaviour.OnDeath(damageOutcome, location);
                if (behaviour.UseDeathAnimation) {
                    useDeathAnim = true;
                    deathAnimType = NpcDeath.GetHigherPriority(deathAnimType, behaviour.UseCustomDeathAnimation);
                    NpcGeneralFSM generalFSM = ParentModel?.TryGetElement<NpcGeneralFSM>();
                    if (generalFSM != null) {
                        generalFSM.IsDyingWithCustomAnimation = true;
                    }
                }
            }

            if (useDeathAnim && ParentModel != null) {
                var animatorSubstateMachine = ParentModel.GetAnimatorSubstateMachine(NpcFSMType.OverridesFSM);
                ((NpcOverridesFSM)animatorSubstateMachine).SetDeathAnimationType(deathAnimType);
                if (deathAnimType is not NpcDeath.DeathAnimType.Custom) {
                    animatorSubstateMachine.SetCurrentState(NpcStateType.Death, force: true);
                }
            }
        }
    }
}
