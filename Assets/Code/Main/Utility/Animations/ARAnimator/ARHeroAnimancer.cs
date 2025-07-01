using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [Il2CppEagerStaticClassConstruction]
    public sealed class ARHeroAnimancer : AnimancerComponent {
        static readonly List<ARHeroStateToAnimationMappingEntry> RemovedEntries = new (10);

        public float heavyAttackMult1H = 0.9f;
        public float lightAttackMult1H = 1f;
        public float heavyAttackMult2H = 0.9f;
        public float lightAttackMult2H = 1f;
        public float bowDrawSpeed = 1f;
        
        [SerializeField] VCTppHeroAnimationRigging animationRigging;
        [Required]
        [SerializeField, HeroAnimancerBaseAnimationsReference]
        [InfoBox("Base animations that are used for Hero and are always loaded. These animations should not be depended from currently equipped Hero weapon.")]
        ShareableARAssetReference baseAnimations;

        RigBuilder _rigBuilder;
        Action _onAnimationsLoaded;
        ARAssetReference _baseAnimationsReference;
        ARHeroAnimancerBaseAnimations _baseAnimations;
        readonly List<ReplacementStackItem> _replacements = new();
        readonly Dictionary<int, AnimancerState> _entryToTransition = new();
        
        public float MovementSpeed => Hero.Current.HorizontalSpeed;
        
        // --- Fallback States
        static readonly Dictionary<HeroStateType, HeroStateType> FallbackStates = new() {
            { HeroStateType.HeavyAttackStartAlternate, HeroStateType.HeavyAttackStart },
            { HeroStateType.HeavyAttackWaitAlternate, HeroStateType.HeavyAttackWait },
            { HeroStateType.HeavyAttackEndAlternate, HeroStateType.HeavyAttackEnd },
        };

        protected override void Awake() {
            base.Awake();
            InitializeHeroAnimancer().Forget();
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
        
        public void RebindAnimationRigging() {
            if (!Animator.enabled) {
                // Don't rebind disabled animator since it makes character enter t-pose.
                return;
            }
            
            if (animationRigging != null) {
                animationRigging.RebindAnimationRigging(Animator, Playable);
            }
        }
        
        void Update() {
            if (_rigBuilder) {
                _rigBuilder.SyncLayers();
            }
        }

        async UniTaskVoid InitializeHeroAnimancer() {
            _baseAnimationsReference = baseAnimations.Get();
            if (_baseAnimationsReference is not { IsSet: true }) {
                Log.Critical?.Error("Hero does not have base animations set!", gameObject);
                return;
            }
            
            var result = await _baseAnimationsReference.LoadAsset<ARHeroAnimancerBaseAnimations>();
            if (result == null) {
                Log.Critical?.Error("Failed to load base animations for Animancer! Hero will be broken!", gameObject);
                return;
            }

            if (this == null || Hero.Current.HasBeenDiscarded) {
                _baseAnimationsReference?.ReleaseAsset();
                _baseAnimationsReference = null;
                return;
            }
            
            _baseAnimations = result;
            foreach (var mapping in _baseAnimations.animationMappings) {
                ApplyOverrides(this, mapping);
            }

            OnAnimationsLoaded();
        }
        
        public AnimationCurve GetAnimationSpeedCurve(HeroLayerType layer, HeroStateType currentStateType) {
            return GetMappingsForLayer(layer).Select(m => m.GetCustomCurve(currentStateType)).FirstOrDefault();
        }
        
        public void ApplyOverrides(object context, ARHeroStateToAnimationMapping replacements) {
            ApplyOverrides(context, replacements.name, replacements);
        }
        
        void ApplyOverrides(object context, string assetName, ARHeroStateToAnimationMapping replacements) {
            Log.Minor?.Info($"Adding Overrides with context: {context}", gameObject);
            RemovedEntries.Clear();
            RemoveReplacements(context, assetName, replacements.layerType, RemovedEntries);
            _replacements.Add(new ReplacementStackItem(context, assetName, replacements));
            OnAddedAnimationsToCollection(replacements);
            RemovedEntries.Clear();
        }

        public void RemoveOverrides(object context, ARHeroStateToAnimationMapping replacements) {
            Log.Debug?.Info($"Removing Overrides with context: {context}", gameObject);
            RemovedEntries.Clear();
            RemoveReplacements(context, replacements.name, replacements.layerType, RemovedEntries);
            OnRemovedAnimationsFromCollection(RemovedEntries, replacements.layerType);
            RemovedEntries.Clear();
        }
        
        void RemoveReplacements(object context, string assetName, HeroLayerType layerType, List<ARHeroStateToAnimationMappingEntry> removedEntries) {
            int hash = ReplacementStackItem.CreateHash(context, assetName, layerType);
            for (int i = 0; i < _replacements.Count; i++) {
                if (_replacements[i].hash == hash) {
                    removedEntries.AddRange(_replacements[i].mapping.entries);
                    _replacements.RemoveAt(i);
                    return;
                }
            }
        }
        
        public void GetAnimancerNode(HeroLayerType heroLayerType, HeroStateType heroStateType, Action<ITransition> onComplete, Action onFail, CancellationToken cancellationToken) {
            if (_baseAnimations == null) {
                _onAnimationsLoaded += () => InternalGetAnimancerNode(heroLayerType, heroStateType, onComplete, onFail, cancellationToken);
                return;
            }
            
            InternalGetAnimancerNode(heroLayerType, heroStateType, onComplete, onFail, cancellationToken);
        }
        
        void InternalGetAnimancerNode(HeroLayerType heroLayerType, HeroStateType heroStateType, Action<ITransition> onComplete, Action onFail, CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }
            
            List<ITransition> transitions = new();
            foreach (var mapping in GetMappingsForLayer(heroLayerType)) {
                transitions.AddRange(AnimancerUtils.GetAnimancerNodes(heroStateType, mapping));
            }

            if (transitions.Count <= 0) {
                if (FallbackStates.TryGetValue(heroStateType, out var fallbackState)) {
#if DEBUG
                    if (DebugReferences.LogAnimancerFallbackState) {
                        Log.Minor?.Warning($"Failed to find animation for state: {heroStateType}. Falling back to: {fallbackState}", gameObject);
                    }
#endif
                    InternalGetAnimancerNode(heroLayerType, fallbackState, onComplete, onFail, cancellationToken);
                    return;
                }
                
                onFail?.Invoke();
                return;
            }
            
            ITransition transition = transitions.Count == 1 ? transitions[0] : RandomUtil.UniformSelect(transitions);
            onComplete.Invoke(transition);
        }
        
        void OnAddedAnimationsToCollection(ARHeroStateToAnimationMapping newMapping) {
            if (newMapping == null) {
                return;
            }

            var heroAnimatorSubstateMachine = Hero.Current.Elements<HeroAnimatorSubstateMachine>()
                .FirstOrDefault(h => h.LayerType == newMapping.layerType);

            SynchronizedHeroSubstateMachine synchronizedHeroSubstateMachine = null;
            foreach (var fsm in Hero.Current.Elements<HeroAnimatorSubstateMachine>()) {
                synchronizedHeroSubstateMachine = fsm.Elements<SynchronizedHeroSubstateMachine>()
                    .FirstOrDefault(s => s.LayerType == newMapping.layerType);
                if (synchronizedHeroSubstateMachine != null) {
                    break;
                }
            }

            if (heroAnimatorSubstateMachine == null && synchronizedHeroSubstateMachine == null) {
                return;
            }
            
            foreach (var mapping in newMapping.entries) {
                // --- Add states to animancer
                foreach (var node in mapping.AnimancerNodes) {
                    if (node is null or MixerTransition2DAsset.UnShared { HasAsset: false }) {
                        Log.Important?.Error("Null node in mapping!", newMapping);
                        continue;
                    }
                    var layer = heroAnimatorSubstateMachine?.AnimancerLayer;
                    if (layer != null) {
                        var state = layer.GetOrCreateState(node);
                        _entryToTransition[ReplacementStackItem.CreateHash(node, mapping, layer)] = state;
                    }

                    var synchronizedLayer = synchronizedHeroSubstateMachine?.AnimancerLayer;
                    if (synchronizedLayer != null) {
                        var synchronizedState = synchronizedLayer.GetOrCreateState(node);
                        _entryToTransition[ReplacementStackItem.CreateHash(node, mapping, synchronizedLayer)] = synchronizedState;
                    } 
                }
            }
        }
        
        void OnRemovedAnimationsFromCollection(IReadOnlyCollection<ARHeroStateToAnimationMappingEntry> removedStates,
            HeroLayerType heroLayerType) {
            if (Hero.Current?.HasBeenDiscarded ?? true) {
                return;
            }

            if (removedStates == null || removedStates.Count == 0) {
                return;
            }

            var substateMachine = Hero.Current.Elements<HeroAnimatorSubstateMachine>()
                .FirstOrDefault(h => h.LayerType == heroLayerType);
            
            SynchronizedHeroSubstateMachine synchronizedHeroSubstateMachine = null;
            foreach (var fsm in Hero.Current.Elements<HeroAnimatorSubstateMachine>()) {
                synchronizedHeroSubstateMachine = fsm.Elements<SynchronizedHeroSubstateMachine>()
                    .FirstOrDefault(s => s.LayerType == heroLayerType);
                if (synchronizedHeroSubstateMachine != null) {
                    break;
                }
            }
            
            foreach (var mapping in removedStates) {
                foreach (var node in mapping.AnimancerNodes) {
                    var layer = substateMachine?.AnimancerLayer;
                    if (layer != null) {
                        int hash = ReplacementStackItem.CreateHash(node, mapping, layer);
                        if (_entryToTransition.TryGetValue(hash, out var state)) {
                            if (state.IsValid) {
                                layer.RemoveState(state);
                            }

                            _entryToTransition.Remove(hash);
                        }
                    }
                    
                    var synchronizedLayer = synchronizedHeroSubstateMachine?.AnimancerLayer;
                    if (synchronizedLayer != null) {
                        int hash = ReplacementStackItem.CreateHash(node, mapping, synchronizedLayer);
                        if (_entryToTransition.TryGetValue(hash, out var synchronizedState)) {
                            if (synchronizedState.IsValid) {
                                synchronizedLayer.RemoveState(synchronizedState);
                            }
                            _entryToTransition.Remove(hash);
                        }
                    }
                }
            }
        }
        
        void OnAnimationsLoaded() {
            if (this == null || Hero.Current.HasBeenDiscarded) {
                return;
            }
            
            _onAnimationsLoaded?.Invoke();
            _onAnimationsLoaded = null;
        }

        // === Helpers
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(Object obj) {
            // Animation Events are handled for hero by HeroWeaponEvents. This method is only to suppress errors from animator.
        }

        IEnumerable<ARHeroStateToAnimationMapping> GetMappingsForLayer(HeroLayerType layerType) {
            return _replacements.Where(m => m.mapping.layerType == layerType).Select(m => m.mapping)
                .Concat(_baseAnimations.animationMappings.Where(m => m.layerType == layerType));
        }
        
        struct ReplacementStackItem {
            public readonly int hash;
            public readonly ARHeroStateToAnimationMapping mapping;

            public ReplacementStackItem(object context, string assetName, ARHeroStateToAnimationMapping mapping) {
                hash = CreateHash(context, assetName, mapping.layerType);
                this.mapping = mapping;
            }

            public static int CreateHash(object context, string assetName, HeroLayerType heroLayerType) {
                return DHash.Combine(context.GetHashCode(), assetName.GetHashCode(StringComparison.InvariantCultureIgnoreCase), heroLayerType.GetHashCode());
            }
            
            public static int CreateHash(object nodeContext, object mappingContext, object layerContext) {
                return DHash.Combine(nodeContext.GetHashCode(), mappingContext.GetHashCode(), layerContext.GetHashCode());
            }
        }
    }
}