using System;
using System.Linq;
using System.Threading;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class RandomInteraction : ForwardingInteractionBase, UnityUpdateProvider.IWithUpdateGeneric, INpcInteractionWrapper {
        [SerializeField, LabelText("List of interactions from Project to be randomized from")] 
        NpcInteractionBase[] interactions = Array.Empty<NpcInteractionBase>();

        [SerializeField] bool swapInteractionWhileInteracting;
        [SerializeField, ShowIf(nameof(swapInteractionWhileInteracting))] float swapInterval = 15f;
        [SerializeField, ShowIf(nameof(swapInteractionWhileInteracting))] float swapIntervalDeviation = 5f;
        
        NpcElement _lastAskingNpc;
        NpcInteractionBase _currentInteractionPrefab;
        NpcInteractionBase _currentInteraction;
        NpcInteractionBase _interactionToRemove;
        NpcElement _currentNpc;
        CancellationTokenSource _removeInteractionToken;
        
        NpcInteractionBase[] _availableInteractions;
        IInteractionFinder _lastFinder;
        bool _updatePaused;
        float _timeToNextInteraction;
        
        float InteractionSwapDelay => swapInterval + RandomUtil.UniformFloat(-swapIntervalDeviation, swapIntervalDeviation);

        public event Action OnInternalEndOverride;

        public override Vector3? GetInteractionPosition(NpcElement npc) => transform.position;
        public override Vector3 GetInteractionForward(NpcElement npc) => transform.forward;
        
        public override INpcInteraction Interaction {
            get {
                if (_currentInteraction == null) {
                    if (_currentInteractionPrefab == null) {
                        return null;
                    }
                    _currentInteraction = CreateInteraction(_currentInteractionPrefab.gameObject);
                }
                return _currentInteraction;
            }
        }

        public override bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            if (_currentNpc != null && _currentNpc != npc) {
                return false;
            }
            if (_availableInteractions is { Length: > 0 }) {
                return true;
            }
            return TryFindAvailableInteractions(npc, finder);
        }
        
        public override void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            if (swapInteractionWhileInteracting) {
                World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
            }
            base.StartInteraction(npc, reason);
        }

        public override void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            // Clear up to force searching a new interaction even for the same NPC
            _currentInteractionPrefab = null;
            base.StopInteraction(npc, reason);
            if (swapInteractionWhileInteracting) {
                World.Services.Get<UnityUpdateProvider>().UnregisterGeneric(this);
            }
        }

        public override InteractionBookingResult Book(NpcElement npc) {
            _currentInteractionPrefab = GetRandomInteraction(npc, _lastFinder);
            var result = base.Book(npc);
            switch (result) {
                case InteractionBookingResult.ProperlyBooked:
                    _currentNpc = npc;
                    break;
                case InteractionBookingResult.AlreadyBookedBySameNpc:
                    break;
                default:
                    RemoveInteraction();
                    break;
            }
            RemoveOldInteraction();
            return result;
        }
        
        public override void Unbook(NpcElement npc) {
            base.Unbook(npc);
            DelayRemoveOldInteraction().Forget();
            _availableInteractions = null;
            _currentInteraction = null;
            _currentNpc = null;
        }
        
        public override event Action OnInternalEnd {
            add => OnInternalEndOverride += value;
            remove => OnInternalEndOverride -= value;
        }
        
        public void OnInternalEndOverrideInvoke() {
            OnInternalEndOverride?.Invoke();
        }
        
        public override bool IsStopping(NpcElement npc) {
            if (_currentInteraction == null) {
                // It can be triggered after Unbook
                return _interactionToRemove != null && _interactionToRemove.IsStopping(npc);
            }
            return _currentInteraction.IsStopping(npc);
        }
        
        bool TryFindAvailableInteractions(NpcElement npc, IInteractionFinder finder) {
            _availableInteractions = interactions.Where(i => i.AvailableFor(npc, finder)).ToArray();
            _lastFinder = finder;
            return _availableInteractions.Any();
        }

        NpcInteractionBase GetRandomInteraction(NpcElement npc, IInteractionFinder finder) {
            if (_availableInteractions.Length == 0) {
                return null;
            }
            if (swapInteractionWhileInteracting) {
                _timeToNextInteraction = InteractionSwapDelay;
            }
            if (InteractionProvider.GetRandomInteraction(_availableInteractions, npc, finder, false) is NpcInteractionBase interactionBase) {
                return interactionBase;
            }
            return null;
        }
        
        public void UnityUpdate() {
            if (IsTalking || _currentInteraction is NpcInteraction { IsUsed: false }) {
                if (!_updatePaused) {
                    _timeToNextInteraction = InteractionSwapDelay;
                    _updatePaused = true;
                }
                return;
            }

            if (_updatePaused) {
                _updatePaused = false;
            }
            
            _timeToNextInteraction -= Time.deltaTime;
            if (_timeToNextInteraction <= 0f) {
                _timeToNextInteraction = InteractionSwapDelay;
                SwapInteraction();
            }
        }
        
        void SwapInteraction() {
            _currentInteraction.StopInteraction(_currentNpc, InteractionStopReason.InteractionFastSwap);
            DelayRemoveOldInteraction().Forget();
            _currentInteractionPrefab = GetRandomInteraction(_currentNpc, _lastFinder);
            _currentInteraction = CreateInteraction(_currentInteractionPrefab.gameObject);
            _currentInteraction.Book(_currentNpc);
            _currentInteraction.StartInteraction(_currentNpc, InteractionStartReason.InteractionFastSwap);
        }
        
        // Creation and removal

        NpcInteractionBase CreateInteraction(GameObject interaction) {
            var interactionInstance = Instantiate(interaction, transform);
            interactionInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            interactionInstance.GetComponentsInChildren<Collider>().ForEach(c => c.enabled = false);
            var npcInteractionBase = interactionInstance.GetComponent<NpcInteractionBase>();
            npcInteractionBase.OnInternalEnd += OnInternalEndOverrideInvoke;
            return npcInteractionBase;
        }

        async UniTaskVoid DelayRemoveOldInteraction() {
            RemoveOldInteraction();
            
            _removeInteractionToken?.Cancel();
            _removeInteractionToken = new CancellationTokenSource();
            
            _interactionToRemove = _currentInteraction;
            _interactionToRemove.OnInternalEnd -= OnInternalEndOverrideInvoke;
            
            (bool isCancelled, bool result) = await AsyncUtil
                .WaitWhile(_interactionToRemove.gameObject, () => _interactionToRemove.IsStopping(_currentNpc), source: _removeInteractionToken)
                .SuppressCancellationThrow();
            if (isCancelled) {
                return;
            }
            
            if (!result) {
                CancelRemoveInteractionToken();
                _interactionToRemove = null;
                return;
            }

            (isCancelled, result) = await AsyncUtil
                .DelayFrame(_interactionToRemove.gameObject, cancellationToken: _removeInteractionToken.Token)
                .SuppressCancellationThrow();
            if (isCancelled) {
                return;
            }
            
            if (!result) {
                CancelRemoveInteractionToken();
                _interactionToRemove = null;
                return;
            }
            
            RemoveOldInteraction();
        }

        void RemoveOldInteraction() {
            CancelRemoveInteractionToken();
            if (_interactionToRemove != null) {
                Destroy(_interactionToRemove.gameObject);
                _interactionToRemove = null;
            }
        }

        void CancelRemoveInteractionToken() {
            _removeInteractionToken?.Cancel();
            _removeInteractionToken = null;
        }
        
        void RemoveInteraction() {
            if (_currentInteraction != null) {
                _currentInteraction.OnInternalEnd -= OnInternalEndOverrideInvoke;
                Destroy(_currentInteraction.gameObject);
                _currentInteraction = null;
            }
            RemoveOldInteraction();
        }

        void OnDestroy() {
            if (swapInteractionWhileInteracting) {
                World.Services.Get<UnityUpdateProvider>().UnregisterGeneric(this);
            }
        }
    }
}
