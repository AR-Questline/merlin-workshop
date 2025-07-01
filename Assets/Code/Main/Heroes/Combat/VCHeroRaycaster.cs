using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class VCHeroRaycaster : ViewComponent<Hero> {
        [SerializeField] RaycastCheck markerPlacementDetection;
        [SerializeField] RaycastCheck npcDetection;
        [SerializeField] InteractionRaycastCheck interactionDetection;
        [SerializeField] RaycastCheck waterDetection;
        [SerializeField] RaycastCheck debugNameDetection;
        
        public RaycastCheck MarkerPlacementDetection => markerPlacementDetection;
        public float npcDetectionMaxDistance = 20f;
        public float waterDetectionMaxDistance = 20f;
        public AnimationCurve raycastCurve;
        public WeakModelRef<Location> NPCRef { get; private set; }
        public Collider NpcCollider { get; private set; }
        
        bool _initialized;
        Transform _transform;

        IInteractableWithHero _interactable;
        IInteractableWithHero _interactableOverride;
        IHeroAction _action;
        bool _isMapInteractive;

        readonly Dictionary<VLocation, IPooledInstance> _debugInstances = new();
        readonly List<VLocation> _locationsToRemove = new();
        
        IHeroInteractionUI InteractionUI => Target.TryGetElement<IHeroInteractionUI>();

        // === Events
        public static class Events {
            public static readonly Event<Hero, Location> PointsTowardsIWithHealthBar = new(nameof(PointsTowardsIWithHealthBar));
            public static readonly Event<Hero, Location> StoppedPointingTowardsLocation = new(nameof(StoppedPointingTowardsLocation));
        }
        
        protected override void OnAttach() {
            _transform = transform;
            _initialized = true;
            interactionDetection.Init();
            var stateStack = UIStateStack.Instance;
            stateStack.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChanged, this);
            _isMapInteractive = stateStack.State.IsMapInteractive;
        }

        void Update() {
            if (!_initialized) {
                return;
            }

            if (!_isMapInteractive) {
                if (!InteractionUI?.HasBeenDiscarded ?? false) {
                    InteractionUI.Discard();
                    _action = null;
                }

                return;
            }

            ThrowRaycast();
            DetermineAction();
        }

        protected override void OnDiscard() {
            _interactable = null;
            _action = null;
        }

        public void SetInteractionOverride(IInteractableWithHero interactable) {
            _interactableOverride = interactable;
        }
        
        public void RemoveInteractionOverride(IInteractableWithHero interactable) {
            if (_interactableOverride != interactable) {
                return;
            }
            _interactableOverride = null;
        }

        public void GetViewRay(out Vector3 position, out Vector3 forward) {
            _transform.GetPositionAndRotation(out position, out var rotation);
            forward = rotation * Vector3.forward;
        }

        public IEnumerable<IHeroAction> GetAvailableActions() {
            return _interactable?.AvailableActions(Hero.Current) ?? Enumerable.Empty<IHeroAction>();
        }
        
        void ThrowRaycast() {
            GetViewRay(out var transformPosition, out var transformForward);

            // --- Npc logic
            NpcCollider = npcDetection.Detected(transformPosition, transformForward, npcDetectionMaxDistance);
            VLocation npcView = NpcCollider != null ? NpcCollider.GetComponentInParent<LocationParent>()?.GetComponentInChildren<VLocation>() : null;
            bool hasNpcView = npcView is { HasBeenDiscarded: false };
            var npc = NPCRef.Get();
            if (hasNpcView && npcView.HasHealthBar && npcView.Target != npc) {
                Target.Trigger(Events.PointsTowardsIWithHealthBar, npcView.Target);
            } else if (npc != null && !hasNpcView) {
                Target.Trigger(Events.StoppedPointingTowardsLocation, null);
            }
            NPCRef = hasNpcView ? npcView.Target : null;
            
            if (DebugProjectNames.Extended) {
                ShowDebugLabels(transformPosition, transformForward);
            }
            
            // --- Player Interaction logic
            _interactable = GetInteractable(transformPosition, transformForward);
            
            // --- Water Interaction logic
            // Performed only if not interaction was found to allow picking up objects in shallow water etc.
            if (_interactable == null) {
                var detected = waterDetection.Detected(transformPosition, transformForward, waterDetectionMaxDistance);
                if (detected) {
                    var provider = detected.GetComponentInParent<IInteractableWithHeroProvider>();
                    _interactable = provider?.InteractableWithHero(detected);
                }
            }
        }

        void ShowDebugLabels(Vector3 transformPosition, Vector3 transformForward) {
            GameObject debugPrefab = World.ExtractPrefab(typeof(LocationDebugName));
            debugNameDetection.CheckMultiHit(transformPosition, transformForward, out var hits, 20f, 0.02f);
                
            _locationsToRemove.Clear();
            _locationsToRemove.AddRange(_debugInstances.Keys);
                
            for (int i = 0; i < hits.Count; i++) {
                var hit = hits[i];
                if (hit.Collider.TryGetComponentInParent(out VLocation locationView) || hit.Collider.TryGetComponentInChildren(out locationView)) {
                    if (!_debugInstances.ContainsKey(locationView)) {
                        _debugInstances.Add(locationView, PrefabPool.Instantiate(debugPrefab, Vector3.zero, Quaternion.identity));
                        var vLocationDebugName = _debugInstances[locationView].Instance.GetComponent<LocationDebugName>();
                        vLocationDebugName.Init(locationView.Target);
                    }
                    _locationsToRemove.Remove(locationView);
                }
            }
            
            for (int i = 0; i < _locationsToRemove.Count; i++) {
                var locationView = _locationsToRemove[i];
                _debugInstances[locationView].Return();
                _debugInstances.Remove(locationView);
            }
        }

        IInteractableWithHero GetInteractable(Vector3 transformPosition, Vector3 transformForward) {
            if (_interactableOverride != null) {
                return _interactableOverride;
            } 
            
            VHeroController vHeroController = Target.VHeroController;
            if (vHeroController.BodyData == null) {
                return null;
            }
            float angle = vHeroController.FirePoint.transform.forward.y * -1;
            float interactionLength = Target.Data.standingHeightData.height;
            if (vHeroController.IsCrouching) {
                interactionLength *= Target.Data.crouchingInteractionLengthMultiplier;
            }
            interactionLength *= raycastCurve.Evaluate(angle);
            var detected = interactionDetection.Detected(transformPosition, transformForward, interactionLength);
            if (detected == null) {
                return null;
            }

            var interactable = detected.GetComponentInParent<IInteractableWithHeroProviderComplex>()?.InteractableWithHero(detected);

            return interactable;
        }

        void DetermineAction() {
            if (_interactable == null || !HeroInteraction.ShouldHappen(Target, _interactable)) {
                InteractionUI?.Discard();
                _action = null;
                return;
            }

            IHeroAction newAction = _interactable.DefaultAction(Hero.Current);

            if (newAction == null) {
                InteractionUI?.Discard();
                _action = null;
            } else if (newAction != _action) {
                _action = newAction;
                ShowInteraction();
            }
        }

        void ShowInteraction() {
            InteractionUI?.Discard();
            Target.AddElement(_action.InteractionUIToShow(_interactable));
        }

        void OnUIStateChanged(UIState state) {
            _isMapInteractive = state.IsMapInteractive;
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            Gizmos.color = interactionDetection.DebugCastData.hit switch {
                true => Color.green,
                false => Color.red,
                null => Color.white
            };
            switch (interactionDetection.DebugCastData.castType) {
                case InteractionRaycastCheck.DebugInteractionCastData.CastType.Ray:
                    Gizmos.DrawLine(interactionDetection.DebugCastData.origin,
                        interactionDetection.DebugCastData.origin + interactionDetection.DebugCastData.direction.normalized *
                        interactionDetection.DebugCastData.distance);
                    break;
                case InteractionRaycastCheck.DebugInteractionCastData.CastType.Sphere:
                    Gizmos.DrawWireSphere(interactionDetection.DebugCastData.origin, interactionDetection.DebugCastData.radius);
                    Gizmos.DrawWireSphere(
                        interactionDetection.DebugCastData.origin + interactionDetection.DebugCastData.direction.normalized *
                        interactionDetection.DebugCastData.distance, interactionDetection.DebugCastData.radius);
                    break;
            }
        }
    #endif
    }
}
