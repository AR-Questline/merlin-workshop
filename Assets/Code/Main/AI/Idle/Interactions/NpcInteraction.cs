using System;
using System.Threading;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    [RequireComponent(typeof(SphereCollider))]
    public abstract class NpcInteraction : NpcInteractionBase {
        const float DefaultWeight = 1;
        const float SpecificActorBonusWeight = 1;
        const string FindingGroup = HideIfIsUniqueGroup + "/Finding";
        
        [InfoBox("Set tags upon which this interaction will be looked for by NPCs nearby (in range).")]
        [SerializeField, FoldoutGroup(FindingGroup, Expanded = true), Tags(TagsCategory.Interaction)] public string[] tags = Array.Empty<string>();
        
        [Tooltip("It makes it available only to a specific actor, and also increases the likelihood of it being chosen by that actor.")]
        [SerializeField, FoldoutGroup(FindingGroup, Expanded = true)] bool onlySpecificActor;
        [SerializeField, FoldoutGroup(FindingGroup, Expanded = true), ShowIf(nameof(onlySpecificActor))] ActorRef specificActor;
        
        [Tooltip("Force this interaction to be selected by the actor, unless another interaction is being forced nearby, in which case it will choose one of the forced ones.")]
        [SerializeField, FoldoutGroup(FindingGroup, Expanded = true), ShowIf(nameof(onlySpecificActor))] bool forceThisInteractionForThisActor;
        
        [Tooltip("Set the default weight of the interaction, the higher it is, the more likely the interaction will be selected")]
        [SerializeField, FoldoutGroup(FindingGroup, Expanded = true)] float defaultWeight = DefaultWeight;
        
        protected NpcElement _lastInteractingNpc;
        protected bool _isUsed;
        NpcCustomActionsFSM _interactingNpcCustomActionsFSM;
        bool _interactingHero;
        CancellationTokenSource _cancellationTokenSource;
        
        float? _overridenWeight;
        bool? _overridenForce;

        protected override bool Editor_HideUnique => tags.Length > 0 && !IsUnique;
        public bool IsUsed => _isUsed;
        public override Vector3? GetInteractionPosition(NpcElement npc) => transform.position;
        public override Vector3 GetInteractionForward(NpcElement npc) => transform.forward;
        protected NpcCustomActionsFSM InteractingNpcCustomActionsFSM => _interactingNpcCustomActionsFSM ??=
            _interactingNpc?.TryGetElement<NpcCustomActionsFSM>();

        public bool AvailableFor(Hero hero) {
            return _interactingNpc == null;
        }
        public override bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return base.AvailableFor(npc, finder)
                   && (_interactingNpc == null || _interactingNpc == npc)
                   && (!onlySpecificActor || specificActor.Get() == npc.Actor)  
                   && AvailableForFinder(finder)
                   && !_interactingHero;
        }

        bool AvailableForFinder(IInteractionFinder finder) {
            if (finder is InteractionBaseFinder baseFinder) {
                return TagUtils.HasRequiredTag(tags, baseFinder.Tag);
            }
            if (finder is InteractionUniqueFinder uniqueFinder) {
                return InteractionUtils.AreSearchablesTheSameInteraction(null, uniqueFinder.Searchable, this);
            }
            Log.Critical?.Error($"Can't check availability for {finder} and {this}");
            return false;
        }

        public float Weight(out bool forced) {
            float weight = _overridenWeight ?? defaultWeight;
            forced = _overridenForce ?? false;
            if (onlySpecificActor) {
                weight += SpecificActorBonusWeight;
                forced = forced || forceThisInteractionForThisActor;
            }
            return weight;
        }

        public void OverrideWeight(float? newWeight, bool? force) {
            _overridenWeight = newWeight;
            _overridenForce = force;
        }

        public void Book(Hero hero) {
            _interactingHero = true;
        }
        
        public override InteractionBookingResult Book(NpcElement npc) {
            if (_interactingNpc != null) {
                if (_interactingNpc == npc) {
                    return InteractionBookingResult.AlreadyBookedBySameNpc;
                }
                Log.Important?.Error($"Trying to book {this} for {npc} while it is already booked for {_interactingNpc}");
                return InteractionBookingResult.AlreadyBookedByOtherNpc;
            }
            
            _interactingNpc = npc;
            _interactingNpcCustomActionsFSM = npc.TryGetElement<NpcCustomActionsFSM>();
            
            return InteractionBookingResult.ProperlyBooked;
        }
        
        public void Unbook(Hero hero) {
            _interactingHero = false;
        }
        public override void Unbook(NpcElement npc) {
            _interactingNpc = null;
            ResetInteractingNpcCustomAnimationsFsm();
            _lastInteractingNpc = npc;
            ClearBookedHistory().Forget();
        }
        
        public bool BookedBy(NpcElement npc) {
            return _interactingNpc == npc;
        }

        public bool WasBookedBy(NpcElement npc) {
            return _lastInteractingNpc == npc || BookedBy(npc);
        }

        public override void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            if (!BookedBy(npc)) {
                Log.Important?.Error($"Cannot interact. Interaction was not booked by {npc}");
                return;
            }
            
            _isUsed = true;
            OnStart(npc, reason);
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _lastInteractingNpc = null;
        }

        public override void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            if (!StartedBy(npc)) {
                Log.Important?.Error($"Cannot stop interaction. Interaction was not started by {npc}");
                return;
            }
            
            OnEnd(npc, reason);
            _isUsed = false;
            
            OnExit(npc);
        }
        
        public override void ResumeInteraction(NpcElement npc, InteractionStartReason reason) {
            if (!BookedBy(npc)) {
                Log.Important?.Error($"Cannot interact. Interaction was not booked by {npc}");
                return;
            }
            
            _isUsed = true;
            OnResume(npc, reason);
        }

        public override void PauseInteraction(NpcElement npc, InteractionStopReason reason) {
            if (!StartedBy(npc)) {
                Log.Important?.Error($"Cannot stop interaction. Interaction was not started by {npc}");
                return;
            }
            
            OnPause(npc, reason);
            _isUsed = false;
            
            OnExit(npc); //TODO ???
        }

        protected void ForcedExit(NpcElement npc) {
            _isUsed = false;
            OnExit(npc);
        }
        
        public bool StartedBy(NpcElement npc) {
            return _isUsed && _interactingNpc == npc;
        }
        
        protected abstract void OnStart(NpcElement npc, InteractionStartReason reason);
        protected abstract void OnEnd(NpcElement npc, InteractionStopReason reason);
        protected virtual void OnResume(NpcElement npc, InteractionStartReason reason) => OnStart(npc, reason);
        protected virtual void OnPause(NpcElement npc, InteractionStopReason reason) => OnEnd(npc, reason);
        protected virtual void OnExit(NpcElement npc) {}

        protected void End() {
            TriggerOnEnd();
        }
        
        async UniTaskVoid ClearBookedHistory() {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            if (await AsyncUtil.DelayTime(this, 8, false, _cancellationTokenSource)) {
                _lastInteractingNpc = null;
            }
        }

        protected void ResetInteractingNpcCustomAnimationsFsm() {
            _interactingNpcCustomActionsFSM = null;
        }
    }
}