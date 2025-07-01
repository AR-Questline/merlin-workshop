using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Idle.Interactions.SimpleInteractionAttachments;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Animations.FSM.Npc.States.Rotation;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using NpcMovement = Awaken.TG.Main.AI.Movement.NpcMovement;
#if UNITY_EDITOR
using Awaken.TG.EditorOnly.Utils;
using UnityEditor;
#endif

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class SimpleInteractionBase : NpcInteractionWithUpdate, ISnappable {
        protected const string AnimationSettingGroup = InteractingGroup + "/Animation Settings";
        static readonly int ExitSpeed = Animator.StringToHash("CustomExitSpeed");

        [SerializeField, PropertyOrder(-1)] bool preventTakingDamage;
        [SerializeField, FoldoutGroup(InteractingGroup, InteractingGroupOrder, Expanded = true)]
        [Tooltip("If true, the interaction will end by itself after a certain time or loop count.\n" +
                 "Otherwise it can be stopped by external conditions (story, NPC routine, fight, etc).")]
        protected bool hasDuration;
        [SerializeField, FoldoutGroup(InteractingGroup), ShowIf(nameof(hasDuration))] DurationType durationType = DurationType.Time;
        [SerializeField, FoldoutGroup(InteractingGroup), ShowIf(nameof(UseTimeDuration))] protected float duration = 15f;
        [SerializeField, FoldoutGroup(InteractingGroup), ShowIf(nameof(UseLoopDuration))] int loopCount = 1;
        [SerializeField, FoldoutGroup(InteractingGroup)] bool useExitAnimation = true;
        [SerializeField, FoldoutGroup(InteractingGroup), HideIf(nameof(useExitAnimation))] float exitDuration = 0.5f;
        
        [SerializeField, FoldoutGroup(AnimationSettingGroup), AnimancerAnimationsAssetReference] ARAssetReference overrides;
        protected ShareableARAssetReference _shareableOverrides;

        [SerializeField, FoldoutGroup(InterruptingGroup, InterruptingGroupOrder)] bool canBePushedFrom = true;
        [SerializeField, FoldoutGroup(InterruptingGroup)] bool canEnterCombat = true;
        [SerializeField, FoldoutGroup(InterruptingGroup)] bool ignoreEnviroDanger = false;
        [SerializeField, FoldoutGroup(InterruptingGroup), ShowIf(nameof(canEnterCombat))] float exitToCombatAnimationSpeed = 3f;
        
        [Tooltip("Is it possible for npc to talk with hero during this interaction? example: sleeping drunk should not be responsive.")]
        [SerializeField, FoldoutGroup(DialogueGroup, DialogueGroupOrder), ShowIf(nameof(AllowTalk))] bool talkInInteraction;

        bool _isStopping;
        bool _forceEndedEffects;
        bool _isTalking;
        bool _fullyEntered;
        bool _hasBeenPaused;
        
        float _endTime;
        int _loopsToEnd;
        IEventListener _loopEndedListener;

        ARInteractionAnimations _arInteractionAnimations;
        IEventListener _unloadOverridesListener;
        IEventListener _customEquipEnteredListener;
        
        CancellationTokenSource _delayedEnterToken, _delayExitToken;
        
        public bool IsTalking => _isTalking;
        public override bool IsStopping(NpcElement npc) => _isStopping;
        public override bool CanBeInterrupted => !_isTalking;
        public override bool CanBePushedFrom => !_isTalking && canBePushedFrom && canEnterCombat;
        public override bool AllowGlancing => false;
        public override bool FullyEntered => _fullyEntered;
        public override bool AllowUseIK => false;
        public override bool CanUseTurnMovement => false;
        public InteractionAnimationData InteractionAnimationData => _arInteractionAnimations?.InteractionAnimationData ?? InteractionAnimationData.Default();
        protected bool AllowEndFromUpdate => !_isTalking;
        public bool CanTalkInInteraction => AllowTalk && talkInInteraction;
        public virtual bool TalkRotateOnlyUpperBody => true;
        public virtual SpineRotationType SpineRotationType => SpineRotationType.None;
        public virtual Transform Transform => transform;
        protected virtual float SnapDuration => SnapToPositionAndRotate.DefaultSnapDuration;
        protected virtual MovementState TargetMovementState => new SnapToPositionAndRotate(SnapToPosition, SnapToForward, gameObject, SnapDuration);
        protected IEnumerable<SimpleInteractionAttachment> InteractionAttachments { get; private set; }

        protected virtual Vector3 SnapToForward => Transform.forward;
        protected virtual Vector3 SnapToPosition => Transform.position;

        bool UseTimeDuration => hasDuration & durationType == DurationType.Time;
        bool UseLoopDuration => hasDuration & durationType == DurationType.LoopCount;

        void Awake() {
            if (overrides is { IsSet: true } && _shareableOverrides is not { IsSet: true }) {
                _shareableOverrides = overrides.AsShareable();
            }
        }

        protected override void OnEnable() {
            if (!Application.isPlaying) return;
            base.OnEnable();
            if (_isUsed) {
                World.Services.Get<UnityUpdateProvider>().RegisterNpcInteraction(this);
            }
        }

        protected override void OnDisable() {
            if (!Application.isPlaying) return;
            World.Services.Get<UnityUpdateProvider>().UnregisterNpcInteraction(this);
            base.OnDisable();
        }

        void OnDestroy() {
            if (_arInteractionAnimations != null) {
#if UNITY_EDITOR
                if (!EditorApplicationUtils.IsLeavingPlayMode)
#endif
                {
                    Log.Important?.Warning($"SimpleInteractionBase is being destroyed while still having animations loaded. Unloading them. {name}");
                }
                _arInteractionAnimations.UnloadOverride();
                _arInteractionAnimations = null;
            }

#if UNITY_EDITOR
            if (World.EventSystem != null) // Unity sometimes calls this in some weird point
#endif
            {
                World.EventSystem.TryDisposeListener(ref _unloadOverridesListener);
                World.EventSystem.TryDisposeListener(ref _customEquipEnteredListener);
            }
        }

        public override InteractionBookingResult Book(NpcElement npc) {
            if (_lastInteractingNpc == npc && _isStopping) {
                if (_delayExitToken != null) {
                    _delayExitToken.Cancel();
                    _delayExitToken = null;
                } else if (_unloadOverridesListener != null) {
                    DelayExit(npc, 0).Forget();
                }
            }
            return base.Book(npc);
        }

        protected override void OnStart(NpcElement npc, InteractionStartReason reason) {
            if (!Application.isPlaying) return;
            if (UseTimeDuration) {
                _endTime = Time.time + duration;
            } else if (UseLoopDuration) {
                _loopsToEnd = loopCount;
            }

            bool npcReturning = _lastInteractingNpc == npc;
            bool stateValidForFastSnap = reason 
                is InteractionStartReason.NPCPresenceDisabled 
                or InteractionStartReason.NPCActivated 
                or InteractionStartReason.NPCReactivatedFromGameLoad
                or InteractionStartReason.InteractionFastSwap;
            bool shouldDoFastSnap = npcReturning || stateValidForFastSnap;
            
            _isStopping = false;
            _hasBeenPaused = false;
            _forceEndedEffects = false;
            _delayExitToken?.Cancel();
            _delayExitToken = null;
            _fullyEntered = false;
            World.EventSystem.TryDisposeListener(ref _customEquipEnteredListener);
            
            BeforeDelayStart(npc);
            
            if (shouldDoFastSnap) {
                npc.Controller.ResetTargetRootRotation();
                NpcRotate.AbortRotationState(npc);
            }
            
            var movementState = shouldDoFastSnap
                ? new SnapToPositionAndRotate(SnapToPosition, SnapToForward, gameObject, 0f)
                : TargetMovementState;
            npc.Movement.InterruptState(movementState);
            npc.Controller.ToggleIdleOnlyRichAIActivity(false);

            if (!shouldDoFastSnap) {
                NpcRotate.TryEnterRotationState(npc, Transform.forward);
            }

            if (_arInteractionAnimations != null) {
                Log.Critical?.Error($"NPC {npc} is starting interaction which is still waiting for animations to end for {_arInteractionAnimations.Npc}");
                _arInteractionAnimations.UnloadOverride();
                World.EventSystem.TryDisposeListener(ref _unloadOverridesListener);
            }
            _arInteractionAnimations = new ARInteractionAnimations(npc, _shareableOverrides);
            _arInteractionAnimations.LoadOverride();
            OnBeginLoadingAnimations(npc.Controller.ARNpcAnimancer);

            npc.NavMeshCuttingSetActive(true);
            npc.SetActiveCollidersForInteractions(true);
            npc.RemoveElementsOfType<SimpleInteractionExitMarker>();
            
            DelayEnter(npc, shouldDoFastSnap).Forget();

            if (!canEnterCombat && !npc.HasElement<BlockEnterCombatMarker>()) {
                npc.AddElement<BlockEnterCombatMarker>();
            }
            if (ignoreEnviroDanger && !npc.HasElement<IgnoreEnviroDangerMarker>()) {
                npc.AddElement<IgnoreEnviroDangerMarker>();
            }
            if (gameObject.activeInHierarchy && enabled) {
                World.Services.Get<UnityUpdateProvider>().RegisterNpcInteraction(this);
            }

            if (preventTakingDamage) {
                npc.AddElement<NpcPreventDamage>();
            }

            if (UseLoopDuration) {
                _loopEndedListener = npc.ListenTo(CustomLoop.Events.CustomLoopEnded, OnLoopEnded, npc);
            }
            
            InteractionAttachments = GetComponents<SimpleInteractionAttachment>();
            InteractionAttachments.ForEach(attachment => attachment.Started(npc));
            CustomEvent.Trigger(gameObject, "InteractionStarted");
        }

        protected virtual void BeforeDelayStart(NpcElement npc) { }

        async UniTaskVoid DelayEnter(NpcElement npc, bool fastSnap) {
            _delayedEnterToken?.Cancel();
            _delayedEnterToken = new CancellationTokenSource();
            
            bool ShouldWait() => _arInteractionAnimations.IsLoadingOverrides || !npc.ARAnimancerLoaded || NpcRotate.IsInRotation(npc);
            if (!await AsyncUtil.WaitWhile(npc, ShouldWait, _delayedEnterToken)) {
                _delayedEnterToken = null;
                return;
            }

            _delayedEnterToken = null;
            
            World.EventSystem.TryDisposeListener(ref _unloadOverridesListener);
            if (fastSnap) {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomLoop, 0f);
                npc.Controller.ARNpcAnimancer.Playable.Evaluate(0.1f);
            } else {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomEnter);
            }

            _fullyEntered = true;
            AfterDelayStart(npc);
        }
        
        protected virtual void AfterDelayStart(NpcElement npc) { }

        protected override void OnPause(NpcElement npc, InteractionStopReason reason) {
            _hasBeenPaused = true;
            base.OnPause(npc, reason);
        }
        protected override void OnEnd(NpcElement npc, InteractionStopReason reason) {
            _fullyEntered = true;
            
            if (!canEnterCombat && npc.TryGetElement<BlockEnterCombatMarker>(out var blockCombatMarker)) {
                npc.RemoveElement(blockCombatMarker);
            }

            if (ignoreEnviroDanger && npc.TryGetElement<IgnoreEnviroDangerMarker>(out var blockFleeMarker)) {
                npc.RemoveElement(blockFleeMarker);
            }

            if (preventTakingDamage && npc.TryGetElement<NpcPreventDamage>(out var prevent)) {
                npc.RemoveElement(prevent);
            }
            
            ForceEndEffects(npc);

            BeforeDelayExit(npc, reason);

            if (reason == InteractionStopReason.Death) {
                npc?.DestroyInteractionCollider();
                return;
            }
            if (npc?.HasBeenDiscarded ?? true) return;
            if (npc.ParentModel?.HasBeenDiscarded ?? true) return;

            bool skipExit = false;
            if (_delayedEnterToken != null) {
                skipExit = true;
                _delayedEnterToken?.Cancel();
                _delayedEnterToken = null;
            }

            npc.NavMeshCuttingSetActive(false);
            npc.SetActiveCollidersForInteractions(false);

            if (reason == InteractionStopReason.MySceneUnloading) {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None, 0);
                OnCustomStateExited(npc);
                return;
            }

            if (!LocationCullingGroup.InActiveLogicBands(npc.CurrentDistanceBand)) {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None, 0);
                OnCustomStateExited(npc);
                return;
            }
            
            var ai = npc.NpcAI;
            if (ai is { InWyrdConversion: true }) {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None, 0);
                OnCustomStateExited(npc);
                return;
            }

            npc.AddElement(new SimpleInteractionExitMarker(InteractionAnimationData));
            World.EventSystem.TryDisposeListener(ref _customEquipEnteredListener);
            
            bool isExitingToCombat = npc.GetCurrentTarget() != null || ai.TryGetHeroVisibility() >= 1 || reason == InteractionStopReason.NPCStartedCombat;
            if (isExitingToCombat && InteractionAnimationData.customEquipWeapon != CustomEquipWeaponType.Default && npc.CanEquipWeaponsThroughBehaviour) {
                var currentBehaviour = npc.EnemyBaseClass?.CurrentBehaviour.Get();
                bool inNotInterruptableBehaviour = currentBehaviour is { CanBeInterrupted: false };
                if (inNotInterruptableBehaviour ||
                    npc.GetAnimatorSubstateMachine(NpcFSMType.GeneralFSM).CurrentAnimatorState is { Type: NpcStateType.CustomEquipWeapon, Entered: true }) {
                    npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None, InteractionAnimationData.blendDuration);
                    World.EventSystem.TryDisposeListener(ref _customEquipEnteredListener);
                } else {
                    _customEquipEnteredListener = npc.ListenTo(NpcCustomEquipWeapon.Events.EnteredCustomEquipWeapon, _ => {
                        npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None, InteractionAnimationData.blendDuration);
                        World.EventSystem.TryDisposeListener(ref _customEquipEnteredListener);
                    });
                }
                World.EventSystem.TryDisposeListener(ref _unloadOverridesListener);
                _isStopping = true;
                _unloadOverridesListener = npc.ListenTo(NpcCustomActionsFSM.Events.CustomStateExited, () => DelayExit(npc).Forget());
                return;
            }
            
            npc.Movement?.Controller.Animator.SetFloat(ExitSpeed, isExitingToCombat ? exitToCombatAnimationSpeed : 1f);

            bool shouldExitInstantly = reason
                is InteractionStopReason.NPCPresenceDisabled
                or InteractionStopReason.NPCReactivated
                or InteractionStopReason.NPCDeactivated
                or InteractionStopReason.StoppedIdlingInstant
                or InteractionStopReason.InteractionFastSwap;

            if (!_hasBeenPaused && shouldExitInstantly) {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None,
                    reason == InteractionStopReason.InteractionFastSwap ? 60 : 0);
                DelayExit(npc).Forget();
            } else if (useExitAnimation && !skipExit) {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.CustomExit);
                World.EventSystem.TryDisposeListener(ref _unloadOverridesListener);
                _isStopping = true;
                _unloadOverridesListener = npc.ListenTo(NpcCustomActionsFSM.Events.CustomStateExited, () => DelayExit(npc).Forget());
            } else {
                npc.SetAnimatorState(NpcFSMType.CustomActionsFSM, NpcStateType.None, exitDuration);
                DelayExit(npc, exitDuration).Forget();
            }
            
            InteractionAttachments.ForEach(attachment => attachment.Ended(npc));
            CustomEvent.Trigger(gameObject, "InteractionEnded");
        }

        protected virtual void BeforeDelayExit(NpcElement npc, InteractionStopReason reason) {}

        async UniTaskVoid DelayExit(NpcElement npc, float exitTime = 0) {
            _isStopping = true;
            World.EventSystem.TryDisposeListener(ref _unloadOverridesListener);
            _delayExitToken?.Cancel();
            _delayExitToken = new CancellationTokenSource();
            if (exitTime == 0 || await AsyncUtil.DelayTime(npc, exitTime, source: _delayExitToken)) {
                OnCustomStateExited(npc);
            } else if (npc.HasBeenDiscarded) {
                _arInteractionAnimations?.UnloadOverride();
                _arInteractionAnimations = null;
            }

            AfterDelayExit();
            _isStopping = false;
        }
        
        protected virtual void AfterDelayExit() {}

        void OnCustomStateExited(NpcElement npc) {
            npc.RemoveElementsOfType<SimpleInteractionExitMarker>();
            NpcMovement npcMovement = npc.Movement;
            if (npcMovement?.HasBeenDiscarded ?? true) return;
            
            if (npcMovement.CurrentState is RagdollMovement ragdollMovement) {
                ragdollMovement.ExitRagdoll(instant: true);
            } else {
                npcMovement.StopInterrupting();
            }

            bool enableRichAI;
            
            //TODO: this makes no sense after idleStack rework because CurrentUnwrappedInteraction is always null or this interaction. It should check "next interaction", not current.
            var interaction = npc.Behaviours.CurrentUnwrappedInteraction;
            if (interaction is SimpleInteraction) { 
                enableRichAI = !npcMovement.Controller.RichAI.reachedDestination;
            } else { 
                enableRichAI = interaction is not TalkInteraction;
            }
            
            npcMovement.Controller.ToggleIdleOnlyRichAIActivity(enableRichAI);
            if (!_arInteractionAnimations.Npc.HasBeenDiscarded) {
                var animancer = _arInteractionAnimations.Npc.Controller.ARNpcAnimancer;
                if (animancer != null) {
                    OnBeginUnloadingAnimations(animancer);
                }
            }

            _arInteractionAnimations.UnloadOverride();
            _arInteractionAnimations = null;
        }

        protected override void OnExit(NpcElement npc) {
            if (UseLoopDuration) {
                World.EventSystem.TryDisposeListener(ref _loopEndedListener);
            }
            World.Services.Get<UnityUpdateProvider>().UnregisterNpcInteraction(this);
        }

        public override bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) {
            if (!CanTalkInInteraction) return false;
            _isTalking = true;
            _interactingNpc.ParentModel.Trigger(StoryInteraction.Events.StoryInteractionToggled,
                StoryInteractionToggleData.Enter(SpineRotationType));
            _interactingNpc.Element<NpcCustomActionsFSM>().StoryLoop = true;
            if (rotateToHero) {
                LookAt(_interactingNpc, GroundedPosition.HeroPosition, false);
            }

            OnStartTalk();
            return true;
        }
        
        public override void EndTalk(NpcElement npc, bool rotReturnToInteraction) {
            if (_interactingNpc == null || !_isTalking) return;
            _isTalking = false;
            _interactingNpc.Element<NpcCustomActionsFSM>().StoryLoop = false;
            
            _interactingNpc.ParentModel.Trigger(StoryInteraction.Events.StoryInteractionToggled,
                StoryInteractionToggleData.Exit(false));

            if (!npc.Interactor.WalkingToPosition) {
                // We shouldn't override movement states if we're currently returning to the interaction
                if (rotReturnToInteraction) {
                    npc.Movement.InterruptState(TargetMovementState);
                    NpcRotate.TryEnterRotationState(npc, Transform.forward);
                } else {
                    npc.Movement.InterruptState(new NoMove());
                }
            }

            OnEndTalk();
        }
        
        protected virtual void OnStartTalk() {}
        
        protected virtual void OnEndTalk() {}

        void OnLoopEnded() {
            _loopsToEnd--;
            if (_loopsToEnd <= 0) {
                End();
                ForceEndEffects(_interactingNpc);
            }
        }
        
        public override void UnityUpdate() {
            if (_interactingNpc is {WasDiscarded: true} or null) {
                ForcedExit(_interactingNpc);
                ForceEndEffects(_interactingNpc);
                return;
            }
            
            if (UseTimeDuration && AllowEndFromUpdate && Time.time > _endTime) {
                End();
                ForceEndEffects(_interactingNpc);
                return;
            }

            switch (InteractingNpcCustomActionsFSM?.CurrentAnimatorState?.Type) {
                case NpcStateType.CustomLoop: { // Enable things
                    EnableEffectsFromUpdate();
                    break;
                }
                case NpcStateType.None or NpcStateType.CustomExit: { // Disable things
                    if (!_forceEndedEffects) {
                        ForceEndEffects(_interactingNpc);
                    }
                    break;
                }
                case null: {
                    var fsm = InteractingNpcCustomActionsFSM;
                    var state = fsm?.CurrentAnimatorState;
                    Log.Important?.Error($"{_interactingNpc} has no animator state. CustomActionFSM: {fsm}, CurrentAnimatorState: {state}");
                    break;
                }
            }
        }

        protected virtual void EnableEffectsFromUpdate() { }

        protected virtual void ForceEndEffects(NpcElement npc) {
            _forceEndedEffects = true;
        }
        
        // === Helpers
        protected virtual void OnBeginLoadingAnimations(ARNpcAnimancer arNpcAnimancer) { }
        protected virtual void OnBeginUnloadingAnimations(ARNpcAnimancer arNpcAnimancer) { }
        
        // === Possible Attachments (EDITOR)
        const string PossibleAttachmentsGroup = "Possible Attachments";
        
        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> s_possibleAttachments;
        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> PossibleAttachments =>
            s_possibleAttachments ??= PossibleAttachmentsUtil.Get(typeof(SimpleInteractionBase));

        [FoldoutGroup(PossibleAttachmentsGroup, order:999, expanded: true), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Common), icon: SdfIconType.StarFill, IconColor = ARColor.EditorLightYellow)]
        PossibleAttachmentsGroup CommonGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Common, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Common] = value;
        }

#if UNITY_EDITOR
        const string BaseHumanPath = "Assets/3DAssets/Characters/Humans/Base_Male/Prefabs/Simple/Prefab_Base_MaleCharSimple.prefab";
        const string PreviewHumanName = "SimpleInteractionPreview";
        float _animTimeStamp;
        GameObject _baseHumanAnimator;
        AnimationClip _clip;

        [Button, PropertyOrder(100), FoldoutGroup("Preview", 100)]
        void SimulatePreview(GameObject customPreviewPrefab) {
            DestroyPreview();
            // --- Load Base Human
            GameObject baseHuman = InstantiatePreview(customPreviewPrefab != null ? customPreviewPrefab : AssetDatabase.LoadAssetAtPath<GameObject>(BaseHumanPath));
            _baseHumanAnimator = baseHuman.GetComponentInChildren<Animator>().gameObject;
            // --- Load Anim
            ARStateToAnimationMapping mapping = overrides.LoadAsset<ARStateToAnimationMapping>().WaitForCompletion();
            _clip = (mapping.GetAnimancerNodes(NpcStateType.CustomLoop) as ClipTransition[])?.FirstOrDefault()?.Clip;
            // --- Play Anim
            _animTimeStamp = 0;
            if (_clip != null) {
                _clip.SampleAnimation(_baseHumanAnimator.gameObject, _animTimeStamp);
            }

            EditorApplication.update += UpdateAnimation;
        }

        [Button, PropertyOrder(101), FoldoutGroup("Preview", 100)]
        void DestroyPreview() {
            EditorApplication.update -= UpdateAnimation;
            var existingPrefab = gameObject.FindChildRecursively(PreviewHumanName);
            if (existingPrefab != null) {
                DestroyImmediate(existingPrefab.gameObject, true);
            }
        }

        GameObject InstantiatePreview(GameObject previewPrefab) {
            GameObject preview = GameObject.Instantiate(previewPrefab, Transform, false);
            preview.name = PreviewHumanName;
            preview.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
            return preview;
        }

        void UpdateAnimation() {
            if (_baseHumanAnimator != null && _clip != null) {
                _animTimeStamp += 0.0166f;
                _animTimeStamp %= _clip.length;
                _clip.SampleAnimation(_baseHumanAnimator.gameObject, _animTimeStamp);
            } else {
                EditorApplication.update -= UpdateAnimation;
            }
        }
#endif
    }

    internal enum DurationType : byte {
        Time,
        LoopCount,
    }
}
