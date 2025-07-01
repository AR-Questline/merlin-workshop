using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Animations.FSM.Npc.States.Custom;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARTransitions;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UniversalProfiling;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [Il2CppEagerStaticClassConstruction]
    public sealed class ARNpcAnimancer : AnimancerComponent {
        const string AngularSpeedReductionParam = "AngularSpeedReduction";
        const int IncreasedVelocityUpdateSpeedWhenAccelerating = 5;
        
        static readonly UniversalProfilerMarker OnAddedAnimationsMarker = new (Color.yellow, $"{nameof(ARNpcAnimancer)}.{nameof(OnAddedAnimationsToCollection)}");
        static readonly UniversalProfilerMarker RemoveAllFightingStylesAnimationsMarker = new (Color.yellow, $"{nameof(ARNpcAnimancer)}.{nameof(RemoveAllFightingStylesAnimations)}");
        
        static List<ARStateToAnimationMappingEntry> s_removedEntries = new (10);
        
        // === Fields
        [SerializeField] VCAnimationRiggingHandler animationRigging;
        NpcElement _npc;
        AnimatedFloat _npcAngularSpeedReduction;
        ARAssetReference _baseAnimationsReference;
        CancellationTokenSource _baseAnimationsToken;
        CancellationTokenSource _combatDataToken;
        RigBuilder _rigBuilder;
        int _lastFrameUpdate;
        float _deltaTimeElapsedSinceLastUpdate;
        bool _isLoadingBaseAnimations;
        bool _isLoadingAdditionalAnimations;
        bool _initializationCallbacksInvoked;
        float _velocityUpdateSpeed = ARMixerTransition.DefaultVelocityUpdateSpeed;
        float _turningUpdateSpeed = ARMixerTransition.DefaultTurningUpdateSpeed;
        
        Action _onAnimationsLoaded;

        NpcFightingStyle _currentFightingStyle;
        AnimationAndBehaviourMappingEntry _currentCombatData;
        Item _currentStatsItem;
        
        // --- Replacements
        ARStateToAnimationMapping _baseAnimations;
        readonly List<ARAssetReference> _fightingStyleReplacementsReferences = new();
        readonly List<ARStateToAnimationMapping> _fightingStyleReplacements = new();
        readonly List<ReplacementStackItem> _externalReplacements = new();
        readonly Dictionary<ARStateToAnimationMappingEntry, int> _clipsRefCounts = new();
        
        // --- Fallback States
        static readonly Dictionary<NpcStateType, NpcStateType> FallbackStates = new() {
            { NpcStateType.Wait, NpcStateType.CombatIdle },
            { NpcStateType.Rest, NpcStateType.CombatIdle },
            { NpcStateType.CombatIdle, NpcStateType.Idle },
            { NpcStateType.DialogueIdle, NpcStateType.Idle },
            
            { NpcStateType.CombatMovement, NpcStateType.Movement },
            
            { NpcStateType.LookAround, NpcStateType.Attract },
            { NpcStateType.Attract, NpcStateType.CombatIdle },
            { NpcStateType.Taunt, NpcStateType.CombatIdle },
            
            { NpcStateType.EquipRangedWeapon, NpcStateType.EquipWeapon},
            
            { NpcStateType.PoiseBreakBackLeft, NpcStateType.GetHit},
            { NpcStateType.PoiseBreakBack, NpcStateType.GetHit},
            { NpcStateType.PoiseBreakFront, NpcStateType.GetHit},
            { NpcStateType.PoiseBreakBackRight, NpcStateType.GetHit},

            { NpcStateType.AlertStartQuick, NpcStateType.AlertStart },
            { NpcStateType.AlertStart, NpcStateType.AlertLookAround },
            { NpcStateType.AlertLookAround, NpcStateType.AlertLookAt },
            { NpcStateType.AlertLookAt, NpcStateType.Idle },
            { NpcStateType.AlertIdle, NpcStateType.Idle },
            { NpcStateType.AlertMovement, NpcStateType.Movement },
            { NpcStateType.AlertExit, NpcStateType.Idle },
            
            { NpcStateType.FearIdle, NpcStateType.Idle },
            { NpcStateType.FearMovement, NpcStateType.Movement },
            
            { NpcStateType.EquipWeaponFromLying, NpcStateType.CustomExit },
            { NpcStateType.EquipWeaponFromCrouching, NpcStateType.CustomExit },
            { NpcStateType.EquipWeaponFromSitting, NpcStateType.CustomExit },
        };
        
        // === Properties
        public bool Visible { get; private set; }
        public float MovementSpeed { get; private set; }
        public float VelocityForward { get; private set; }
        public float VelocityHorizontal { get; private set; }
        public float AngularVelocity { get; private set; }
        public float AngularSpeedMultiplier { get; private set; } = 1;
        public override AnimancerPlayable Playable {
            get {
                InitPlayable();
                return _Playable;
            }
        }
        
        // === Initialization
        protected override void Awake() {
            base.Awake();
            if (animationRigging == null) {
                animationRigging = GetComponentInChildren<VCAnimationRiggingHandler>(true);
            }

            _rigBuilder = GetComponent<RigBuilder>();
            InitPlayable();
            AnimancerDisposeTracker.StopTracking(_rigBuilder);
        }

        protected override void Start() {
            base.Start();
            AnimancerDisposeTracker.StopTracking(_rigBuilder);
        }

        void InitPlayable() {
            if (IsPlayableInitialized) {
                return;
            }

            _rigBuilder = GetComponent<RigBuilder>();
            if (_rigBuilder != null) {
                _rigBuilder.enabled = false;
                _rigBuilder.Build();

                var wasInitialized = _rigBuilder.didAwake || _rigBuilder.didStart;
                if (!wasInitialized) {
                    AnimancerDisposeTracker.StartTracking(_rigBuilder);
                }

                InitializePlayable(AnimancerPlayable.Create(_rigBuilder.graph));
            } else {
                InitializePlayable();
            }
        }

        protected override void OnInitializePlayable() {
            base.OnInitializePlayable();
            Playable.KeepChildrenConnected = true;
        }

        async UniTask AwaitForSubstateMachinesInit() {
            var machines = _npc.Elements<NpcAnimatorSubstateMachine>();
            while (true) {
                foreach (var machine in machines) {
                    if (machine.CurrentAnimatorState?.CurrentState != null) {
                        return;
                    }
                }
                if (!await AsyncUtil.DelayFrame(_npc, 1)) {
                    return;
                }
            }
        }

        void DisableVisualsForInit() {
            _npc.DisableKandra();
        }

        void EnableVisualsAfterInit() {
            _npc.EnableKandra();
        }

        // === Updating
        void OnUpdate(float deltaTime) {
            if (_npc.IsVisible) {
                AngularSpeedMultiplier = math.max(0, 1 - _npcAngularSpeedReduction.Value);

                if (_rigBuilder) {
                    _rigBuilder.SyncLayers();
                }
            }
        }
        
        // === Fighting Styles
        public async UniTaskVoid InitializeNpcWithFightingStyle(NpcElement npcElement, NpcFightingStyle npcFightingStyle) {
            if (npcFightingStyle == null) {
                throw new Exception("Trying to initialize with null fighting style!");
            }
            
            _npc = npcElement;
            _npcAngularSpeedReduction = new AnimatedFloat(this, AngularSpeedReductionParam);

            Visible = LocationCullingGroup.InNpcVisibilityBand(_npc.ParentModel.GetCurrentBandSafe(LocationCullingGroup.LastBand));
            if (Visible) {
                DisableVisualsForInit();
                await UniTask.WhenAll(UpdateFightingStyle(npcFightingStyle), AwaitForSubstateMachinesInit());
                if (!await AsyncUtil.DelayFrame(_npc, 1)) {
                    return;
                }
                EnableVisualsAfterInit();
            } else {
                await UpdateFightingStyle(npcFightingStyle);
            }

            if (_npc is not { HasBeenDiscarded: false }) {
                return;
            }
            _npc.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            _npc.ListenTo(Model.Events.BeforeDiscarded, BeforeNpcDiscarded, _npc);
            _npc.ParentModel.ListenTo(NpcElement.Events.AfterNpcVisibilityChanged, OnDistanceBandChanged, _npc);
        }

        void OnDistanceBandChanged(bool visible) {
            if (visible) {
                Visible = true;
                _isLoadingBaseAnimations = true;
                foreach (NpcAnimatorSubstateMachine fsm in _npc.Elements<NpcAnimatorSubstateMachine>()) {
                    fsm.OnAnimancerVisibilityToggled(Visible);
                }
                OnBecomeVisible().Forget();
            } else {
                Visible = false;
                foreach (NpcAnimatorSubstateMachine fsm in _npc.Elements<NpcAnimatorSubstateMachine>()) {
                    fsm.OnAnimancerVisibilityToggled(Visible);
                }
                ReleaseCurrentBaseAnimations(out _);
                RemoveCombatDataAnimations();
                foreach (var layer in Layers) {
                    layer.DestroyCurrentState();
                }
            }
        }

        async UniTaskVoid OnBecomeVisible() {
            DisableVisualsForInit();
            var baseTask = SetCurrentBaseAnimations(_currentFightingStyle.BaseAnimations);
            var combatDataTask = UpdateCurrentCombatData(_currentCombatData, _currentStatsItem);
            await UniTask.WhenAll(baseTask, combatDataTask, AwaitForSubstateMachinesInit());
            if (!await AsyncUtil.DelayFrame(_npc, 1)) {
                return;
            }
            EnableVisualsAfterInit();
        }

        public async UniTask UpdateFightingStyle(NpcFightingStyle fightingStyle) {
            _currentFightingStyle = fightingStyle;
            if (!Visible) {
                return;
            }
            await SetCurrentBaseAnimations(_currentFightingStyle.BaseAnimations);
        }

        public async UniTask UpdateCurrentCombatData(AnimationAndBehaviourMappingEntry combatData, Item statsItem) {
            _currentCombatData = combatData;
            _currentStatsItem = statsItem;
            if (_currentCombatData == null) {
                RemoveCombatDataAnimations();
                return;
            }
            if (!Visible) {
                return;
            }
            await UpdateCombatDataAnimations(combatData, statsItem);
        }
        
        async UniTask SetCurrentBaseAnimations(ShareableARAssetReference baseAnimationsReference) {
            ReleaseCurrentBaseAnimations(out CancellationToken cancellationToken);
            _baseAnimationsReference = baseAnimationsReference.Get();
            if (_baseAnimationsReference is not { IsSet: true }) {
                _isLoadingBaseAnimations = false;
                return;
            }

            _isLoadingBaseAnimations = true;
            (bool canceled, var result) = await _baseAnimationsReference.LoadAsset<ARStateToAnimationMapping>()
                .ToUniTask()
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();
            
            if (canceled || cancellationToken.IsCancellationRequested || gameObject == null) {
                return;
            }
            
            if (result == null) {
                Log.Critical?.Error("Failed to load base animations for Animancer! This NPC will not work properly!", gameObject);
                return;
            }
            
            _baseAnimations = result;
            OnAddedAnimationsToCollection(_baseAnimations.entries, _baseAnimations);
            _isLoadingBaseAnimations = false;
            
            if (!_isLoadingAdditionalAnimations) {
                OnAnimationsLoaded();
            }
        }

        void ReleaseCurrentBaseAnimations(out CancellationToken newCancellationToken) {
            AsyncUtil.CancelAndCreateToken(ref _baseAnimationsToken, out newCancellationToken);
            if (_baseAnimations != null) {
                s_removedEntries.Clear();

                foreach (ARStateToAnimationMappingEntry e in _baseAnimations.entries) {
                    if (TryRemoveAnimation(e)) {
                        s_removedEntries.Add(e);
                    }
                }
                
                OnRemovedAnimationsFromCollection(s_removedEntries, forceRemoveAnimations: true);
                s_removedEntries.Clear();
            }
            _baseAnimations = null;
            
            _baseAnimationsReference?.ReleaseAsset();
            _baseAnimationsReference = null;
        }

        async UniTask UpdateCombatDataAnimations(AnimationAndBehaviourMappingEntry combatData, Item itemUsed) {
            if (combatData == null) {
                RemoveCombatDataAnimations();
                return;
            }

            List<ARAssetReference> toAdd = new();

            foreach (var animations in combatData.Animations) {
                toAdd.Add(animations.Get());
            }
            
            if (itemUsed != null) {
                foreach (var conditionalAnimations in combatData.ConditionalAnimations(itemUsed)) {
                    toAdd.Add(conditionalAnimations.Get());
                }
            }

            // --- We are not changing anything so don't do anything
            if (_fightingStyleReplacementsReferences.Count == toAdd.Count &&
                _fightingStyleReplacementsReferences.All(toAdd.Contains)) {
                return;
            }

            AsyncUtil.CancelAndCreateToken(ref _combatDataToken, out CancellationToken cancellationToken);
            _isLoadingAdditionalAnimations = true;
            
            RemoveAllFightingStylesAnimations();
            List<UniTask<ARStateToAnimationMapping>> tasks = new();
            foreach (var assetRef in toAdd) {
                _fightingStyleReplacementsReferences.Add(assetRef);
                tasks.Add(assetRef.LoadAsset<ARStateToAnimationMapping>().ToUniTask());
            }

            (bool canceled, ARStateToAnimationMapping[] results) = await UniTask.WhenAll(tasks)
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (canceled || cancellationToken.IsCancellationRequested || gameObject == null) {
                _isLoadingAdditionalAnimations = false;
                return;
            }

            if (results == null) {
                _isLoadingAdditionalAnimations = false;
                Log.Critical?.Error("Failed to UpdateFightingStyle for Animancer! This NPC will not work properly!", gameObject);
                return;
            }

            foreach (var animationMapping in results) {
                OnAddedAnimationsToCollection(animationMapping.entries, animationMapping);
                _fightingStyleReplacements.Add(animationMapping);
            }
            
            _isLoadingAdditionalAnimations = false;
            
            if (!_isLoadingBaseAnimations) {
                OnAnimationsLoaded();
            }
        }

        void RemoveCombatDataAnimations() {
            AsyncUtil.CancelAndCreateToken(ref _combatDataToken, out _);
            RemoveAllFightingStylesAnimations();
            if (!_isLoadingBaseAnimations) {
                OnAnimationsLoaded();
            }
        }
        
        void RemoveAllFightingStylesAnimations() {
            using var marker = RemoveAllFightingStylesAnimationsMarker.Auto();
            
            _fightingStyleReplacementsReferences.ForEach(r => r.ReleaseAsset());
            _fightingStyleReplacementsReferences.Clear();

            s_removedEntries.Clear();
            foreach (var replacement in _fightingStyleReplacements) {
                foreach (var entry in replacement.entries) {
                    if (TryRemoveAnimation(entry)) {
                        s_removedEntries.Add(entry);
                    }
                }
            }
            OnRemovedAnimationsFromCollection(s_removedEntries, forceRemoveAnimations: !Visible);
            s_removedEntries.Clear();
            _fightingStyleReplacements.Clear();
        }
        
        // === External Overrides
        public void ApplyOverrides(object context, ARStateToAnimationMapping replacements) {
            ApplyOverrides(context, replacements.name, replacements.entries);
        }
        
        void ApplyOverrides(object context, string assetName, List<ARStateToAnimationMappingEntry> replacements) {
            Log.Debug?.Info($"Adding Overrides with context: {context}", gameObject);
            s_removedEntries.Clear();
            RemoveReplacements(context, assetName, s_removedEntries);
            _externalReplacements.Add(new ReplacementStackItem(context, assetName, replacements));
            OnAddedAnimationsToCollection(replacements, context as Object);
            s_removedEntries.Clear();
        }

        public void RemoveOverrides(object context, ARStateToAnimationMapping replacements, Action onAllAnimationsRemoved = null) {
            Log.Minor?.Info($"Removing Overrides with context: {context}", gameObject);
            s_removedEntries.Clear();
            RemoveReplacements(context, replacements.name, s_removedEntries);
            OnRemovedAnimationsFromCollection(s_removedEntries, onAllAnimationsRemoved);
            s_removedEntries.Clear();
        }
        
        void RemoveReplacements(object context, string assetName, List<ARStateToAnimationMappingEntry> removedEntries) {
            int hash = ReplacementStackItem.CreateHash(context, assetName);
            for (int i = 0; i < _externalReplacements.Count; i++) {
                if (_externalReplacements[i].hash == hash) {
                    foreach (var entry in _externalReplacements[i].replacements) {
                        if (TryRemoveAnimation(entry)) {
                            removedEntries.Add(entry);
                        }
                    }
                    _externalReplacements.RemoveAt(i);
                    return;
                }
            }
        }
        
        // === Velocity
        public void UpdateVelocity(float desiredMovementSpeed, float desiredVerticalVelocity, float desiredHorizontalVelocity, float deltaTime, bool updateOnlyVertical) {
            float updateSpeed = deltaTime * _velocityUpdateSpeed;

            if (MovementSpeed > desiredMovementSpeed) {
                updateSpeed *= IncreasedVelocityUpdateSpeedWhenAccelerating;
            }

            MovementSpeed = Mathf.Lerp(MovementSpeed, desiredMovementSpeed, updateSpeed);
            VelocityForward = Mathf.Lerp(VelocityForward, updateOnlyVertical ? desiredMovementSpeed : desiredVerticalVelocity, updateSpeed);
            VelocityHorizontal = updateOnlyVertical ? 0 : Mathf.Lerp(VelocityHorizontal, desiredHorizontalVelocity, updateSpeed);
        }

        public void UpdateAngularVelocity(float desiredAngularVelocity, float deltaTime) {
            float updateSpeed = deltaTime * _turningUpdateSpeed;
            AngularVelocity = Mathf.Lerp(AngularVelocity, desiredAngularVelocity, updateSpeed);
        }
        
        public void RefreshUpdateSpeedsForState(AnimancerState state) {
            if (state is IARMixerState arMixerState) {
                _velocityUpdateSpeed = arMixerState.Properties.velocityUpdateSpeed;
                _turningUpdateSpeed = arMixerState.Properties.turningUpdateSpeed;
            } else {
                _velocityUpdateSpeed = ARMixerTransition.DefaultVelocityUpdateSpeed;
                _turningUpdateSpeed = ARMixerTransition.DefaultTurningUpdateSpeed;
            }
        }

        public void GetAnimancerNode(NpcStateType npcStateType, Action<ITransition> onComplete, Action onFail, bool logErrors = true) {
            if (_baseAnimations == null || _isLoadingAdditionalAnimations) {
                _onAnimationsLoaded += () => InternalGetAnimancerNode(npcStateType, onComplete, onFail, logErrors);
                return;
            }
            
            InternalGetAnimancerNode(npcStateType, onComplete, onFail, logErrors);
        }
        
        public void ForceGetAnimancerNode(NpcStateType npcStateType, Action<ITransition> onComplete, Action onFail, bool logErrors = true) {
            InternalGetAnimancerNode(npcStateType, onComplete, onFail, logErrors);
        }

        void InternalGetAnimancerNode(NpcStateType npcStateType, Action<ITransition> onComplete, Action onFail, bool logErrors) {
            if (!Visible) {
                OnFail(false);
                return;
            }

            var result = new StructList<ITransition>(16);
            for (int i = 0; i < _externalReplacements.Count; i++) {
                var replacement = _externalReplacements[i];
                var transitions = AnimancerUtils.GetAnimancerNodes(npcStateType, replacement.replacements);
                foreach (var t in transitions) {
                    result.Add(t);
                }
            }
            if (result.Count <= 0) {
                for (int i = 0; i < _fightingStyleReplacements.Count; i++) {
                    var replacement = _fightingStyleReplacements[i];
                    var transitions = replacement.GetAnimancerNodes(npcStateType);
                    foreach (var t in transitions) {
                        result.Add(t);
                    }
                }
            }
            if (result.Count <= 0) {
                foreach (var t in _baseAnimations.GetAnimancerNodes(npcStateType)) {
                    result.Add(t);
                }
            }
            
            if (result.Count <= 0) {
                if (FallbackStates.TryGetValue(npcStateType, out var fallbackState)) {
#if DEBUG
                    if (DebugReferences.LogAnimancerFallbackState) {
                        Log.Minor?.Warning($"Failed to find animation for state: {npcStateType}. Falling back to: {fallbackState}", gameObject);
                    }
#endif
                    InternalGetAnimancerNode(fallbackState, onComplete, onFail, logErrors);
                    return;
                }

                OnFail(logErrors);
                return;
            }

            ITransition transition = result.Count == 1 ? result[0] : RandomUtil.UniformSelect(result);
            onComplete.Invoke(transition);
            return;

            void OnFail(bool log) {
                onFail?.Invoke();
                if (log) {
                    Log.Minor?.Error($"Failed to find animation for state: {npcStateType}, Npc: {_npc}", gameObject);
                }
            }
        }
        
        protected override void OnDisable() {
            // Call events as that may release some resources
            foreach (var state in States) {
                state.Events?.OnEnd?.Invoke();
            }
            
            if (_isLoadingBaseAnimations) {
                _npc?.TryGetElement<NpcCustomActionsFSM>()?.TryGetElement<CustomExit>()?.OnAnimatorDisabled();
            }

            if (IsPlayableInitialized) {
                _Playable.PauseGraph();
            }
        }

        public void OnNpcDeath() {
            _baseAnimationsToken?.Cancel();
            _baseAnimationsToken = null;
            _baseAnimations = null;

            _baseAnimationsReference?.ReleaseAsset();
            _baseAnimationsReference = null;
            
            _combatDataToken?.Cancel();
            _combatDataToken = null;
            
            _fightingStyleReplacementsReferences.ForEach(r => r.ReleaseAsset());
            _fightingStyleReplacementsReferences.Clear();

            _currentFightingStyle = null;
            
            _npc?.GetTimeDependent()?.WithoutUpdate(OnUpdate);

            foreach (var layer in Layers) {
                layer.DestroyStates();
            }
        }
        
        protected override void OnDestroy() {
            OnNpcDeath();
            base.OnDestroy();
            if (_rigBuilder != null) {
                _rigBuilder.Clear();
                _rigBuilder = null;
            }
            _npc = null;
        }

        void BeforeNpcDiscarded(Model _) {
            _npc.GetTimeDependent()?.WithoutUpdate(OnUpdate);
        }
        
        // === Fake methods to suppress errors  "Animation event has no receiver, are you missing a component"
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void Hit(){}
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void Finish(){}
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void Combat(){}
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(string evt) {}
        
        // === Helpers
        void OnAddedAnimationsToCollection(IEnumerable<ARStateToAnimationMappingEntry> addedStates, Object context = null) {
            if (_npc == null || _npc.HasBeenDiscarded) {
                return;
            }

            if (addedStates == null) {
                return;
            }

            using var marker = OnAddedAnimationsMarker.Auto();
            
            foreach (var mapping in addedStates) {
                // --- Add animation to used clips dictionary
                if (_clipsRefCounts.TryGetValue(mapping, out int counter)) {
                    _clipsRefCounts[mapping] = counter + 1;
                } else {
                    _clipsRefCounts.Add(mapping, 1);
                }

                // --- Add states to animancer
                foreach (var substateMachine in _npc.Elements<NpcAnimatorSubstateMachine>()) {
                    if (!substateMachine.HasState(mapping.npcStateType)) {
                        continue;
                    }

                    foreach (var node in mapping.AnimancerNodes) {
                        if (node == null || node is Object obj && obj == null) {
                            Log.Important?.Error("Null animation clip in Animancer mapping! " + LogUtils.GetDebugName(_npc) + " : " + gameObject.PathInSceneHierarchy(), gameObject);
                            continue;
                        }
#if UNITY_EDITOR
                        try {
#endif
                            substateMachine.AnimancerLayer.GetOrCreateState(node);
#if UNITY_EDITOR
                        } catch (Exception e) {
                            Log.Critical?.Error($"Exception below happened while trying to load node: {node} from mapping: {mapping}", context);
                            Debug.LogException(e);
                        }
#endif
                    }
                }
            }

            OnAnimancerStatesChanged();
        }

        void OnRemovedAnimationsFromCollection(IReadOnlyCollection<ARStateToAnimationMappingEntry> removedStates, Action onAllAnimationsRemoved = null, bool forceRemoveAnimations = false) {
            if (_npc == null || _npc.HasBeenDiscarded) {
                return;
            }

            if (removedStates == null) {
                return;
            }

            if (removedStates.Count == 0) {
                OnAnimancerStatesChanged();
                onAllAnimationsRemoved?.Invoke();
                return;
            }
            
            bool waitsForAnimationEnd = false;
            bool isAnimancerActive = !forceRemoveAnimations && IsPlaying() && gameObject.activeInHierarchy && this.isActiveAndEnabled;
            
            foreach (var transition in removedStates.SelectMany(r => r.AnimancerNodes)) {
                ARMixerTransition mixerTransition = null;
                if (transition is ARMixerTransition arMixerTransition) {
                    mixerTransition = arMixerTransition;
                } else if (transition is MixerTransition2DAsset { Transition: ARMixerTransition assetTransition }) {
                    mixerTransition = assetTransition;
                }
                
                if (mixerTransition != null) {
                    foreach (var entry in mixerTransition.TurningOverrides.entries) {
                        while (States.TryGet(entry.clip, out var state)) {
                            if (!DestroyStateRecursive(state)) {
                                break;
                            }
                        }
                    }
                }
                
                while (States.TryGet(transition, out var state)) {
                    if (!DestroyStateRecursive(state)) {
                        break;
                    }
                }
                continue;

                bool DestroyStateRecursive(AnimancerState stateToDestroy) {
                    if (States.TryGet(stateToDestroy, out var recursiveState)) {
                        DestroyStateRecursive(recursiveState);
                    }

                    bool stateIsValidAndPlaying = stateToDestroy is { IsValid: true, IsPlaying: true } &&
                                                  (stateToDestroy.Clip != null || stateToDestroy is MixerState<Vector2>);
                    if (isAnimancerActive && stateIsValidAndPlaying) {
                        var events = new AnimancerEvent.Sequence();
                        Action onEnd = null;
                        onEnd = () => OnEnd(stateToDestroy, onEnd).Forget();
                        events.OnEnd += onEnd;
                        stateToDestroy.Events = events;
                        waitsForAnimationEnd = true;
                        return false;
                    }
                    stateToDestroy.Destroy();
                    return true;
                }
            }
            
            OnAnimancerStatesChanged();
            if (!waitsForAnimationEnd) {
                onAllAnimationsRemoved?.Invoke();
            }
            return;

            async UniTaskVoid OnEnd(AnimancerState stateThatEnded, Action toDelete) {
                if (stateThatEnded.IsValid) {
                    stateThatEnded.Events.OnEnd -= toDelete;
                }

                bool clipCondition = stateThatEnded.Clip != null || stateThatEnded is MixerState<Vector2>;
                await UniTask.WaitWhile(() =>
                    this != null 
                    && this.isActiveAndEnabled 
                    && stateThatEnded.IsValid 
                    && clipCondition
                    && stateThatEnded.IsPlaying);
#if UNITY_EDITOR
                    if (EditorOnly.Utils.EditorApplicationUtils.IsLeavingPlayMode) {
                        return;
                    }
#endif
                stateThatEnded.Destroy();
                await UniTask.Yield(PlayerLoopTiming.LastUpdate);
                if (Animator) {
                    OnAnimancerStatesChanged();
                }
                onAllAnimationsRemoved?.Invoke();
            }
        }
        
        void OnAnimationsLoaded() {
            if (this == null || _npc.HasBeenDiscarded) {
                return;
            }
            
            EnableAnimatorLayers();
            _onAnimationsLoaded?.Invoke();
            _onAnimationsLoaded = null;
        }
        
        void EnableAnimatorLayers() {
            if (_initializationCallbacksInvoked || _npc == null || _npc.HasBeenDiscarded) {
                return;
            }
            
            _npc.OnARAnimancerLoaded();
            if (isActiveAndEnabled) {
                foreach (var fsm in _npc.Elements<NpcAnimatorSubstateMachine>()) {
                    fsm.AnimancerEnabled();
                }
            }

            _initializationCallbacksInvoked = true;
        }

        void OnAnimancerStatesChanged() {
            foreach (var state in _npc.Elements<NpcAnimatorState>()) {
                state.OnStatesCollectionChanged(this);
            }

            if (!Animator.enabled) {
                return;
            }

            if (!isActiveAndEnabled) {
                return;
            }

            if (!Visible) {
                return;
            }

            RebindAnimationRigging();
        }

        public void RebindAnimationRigging() {
            if (!Animator.enabled) {
                // Don't rebind disabled animator since it makes character enter t-pose.
                return;
            }
            
            if (animationRigging != null) {
                animationRigging.UpdateRigsWeights(Animator, Playable);
            }
        }

        /// <returns>Returns true if animation is no longer used by anybody and can be removed from animancer</returns>
        bool TryRemoveAnimation(ARStateToAnimationMappingEntry entryToRemove) {
            if (_clipsRefCounts.TryGetValue(entryToRemove, out int counter)) {
                counter -= 1;
                if (counter <= 0) {
                    _clipsRefCounts.Remove(entryToRemove);
                    return true;
                } else {
                    _clipsRefCounts[entryToRemove] = counter;
                    return false;
                }
            }
            return false;
        }

        struct ReplacementStackItem {
            public readonly int hash;
            public readonly List<ARStateToAnimationMappingEntry> replacements;

            public ReplacementStackItem(object context, string assetName, List<ARStateToAnimationMappingEntry> replacements) {
                hash = CreateHash(context, assetName);
                this.replacements = replacements;
            }

            public static int CreateHash(object context, string assetName) {
                return DHash.Combine(context.GetHashCode(), assetName.GetHashCode(StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public enum NpcLayers : byte {
            General = 0,
            Additive = 1,
            CustomActions = 2,
            TopBody = 3,
            Overrides = 4,
        }
    }
}