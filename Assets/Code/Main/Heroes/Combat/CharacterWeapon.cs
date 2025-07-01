using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Finishers;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Main.Heroes.Combat {
    public class CharacterWeapon : CharacterHand {
        const float EnviroHitRange = 0.35f;
        public const float AngularSpeedModifierDurationExtend = 0.5f;
        static readonly string[] OffHandLayers = { MagicMeleeOffHandFSM.LayerName };
        
        [FoldoutGroup("Hero Settings"), HeroAnimancerAnimationsAssetReference, PropertyOrder(-1)] 
        public ARAssetReference offHandAnimatorControllerRef, offHandAnimatorControllerRefTpp;
        [FoldoutGroup("Hero Settings"), ARAssetReferenceSettings(new[] { typeof(FinishersList) }, group: AddressableGroup.AnimatorOverrides), PropertyOrder(-2)]
        public ARAssetReference finisherList, finisherListTpp;
        [FoldoutGroup("Hero Settings"), ARAssetReferenceSettings(new[] { typeof(FinishersList) }, group: AddressableGroup.AnimatorOverrides), PropertyOrder(-2)]
        public ARAssetReference executionList, executionListTpp;
        [FoldoutGroup("Hero Settings"), Range(0.1f, 3f)]
        public float animatorSpeed = 1f;
        [field: SerializeField, FoldoutGroup("Hero Settings")]
        public HitStopsAsset HitStopsAssetOverride { get; private set; }
        [FoldoutGroup("Collider Settings"), ReadOnly] public Transform colliderParent;
        [FoldoutGroup("Collider Settings"), SerializeField] Vector3 size;
        [FoldoutGroup("Collider Settings"), SerializeField] float additionalLengthForHero = 0.5f;
        [FoldoutGroup("Collider Settings"), SerializeField] float additionalLengthForNpcWhenFightingHero = 0.5f;
        [FoldoutGroup("Trail Settings"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        ShareableARAssetReference weaponTrailOverride;
        [FoldoutGroup("Trail Settings"), SerializeField] bool useWeaponTrail = true;
        [FoldoutGroup("Npc Settings"), Range(0f, 1f), SerializeField] float angularSpeedModifierWhenAttacking = 0.25f;
        
        // === Fields
        protected bool _equippedInMainHand;
        readonly List<IEventListener> _listeners = new();
        // --- Character Fields
        Vector3 _moveDirection, _lastUpdatePosition, _lastUpdateForward;
        Quaternion _lastUpdateRotation;
        bool _attached;
        AttackType _attackType;
        bool _inAttack;
        readonly RaycastHit[] _hitResults = new RaycastHit[32];
        // --- Weapon Trail
        IPooledInstance _spawnedTrail;
        //XWeaponTrail _weaponTrail;
        // --- Speeding up animation
        WeaponRestriction _currentRestriction;
        AnimancerLayer _currentAnimLayer;
        AnimationCurve _speedMultiplyCurve;
        // --- Detecting Enviro Hits
        readonly RaycastHit[] _enviroHits = new RaycastHit[1];
        bool _attachedToHero;
        // --- Attack Params
        AttackParameters _currentAttackParameters;
        
        // === Animations Settings
        protected override string[] LayersToEnable => _equippedInMainHand ? base.LayersToEnable : OffHandLayers;
        protected override ARAssetReference AnimatorControllerRef => _equippedInMainHand ? base.AnimatorControllerRef : OffHandAnimatorControllerRef;
        
        ARAssetReference _cachedOffHandAnimatorControllerRef;
        ARAssetReference _cachedOffHandAnimatorControllerRefTpp;
        ARAssetReference OffHandAnimatorControllerRef {
            get {
                if (Hero.TppActive) {
                    if (offHandAnimatorControllerRefTpp?.IsSet ?? false) {
                        return offHandAnimatorControllerRefTpp;
                    }
                    return _cachedOffHandAnimatorControllerRefTpp ??= Services.Get<GameConstants>().defaultMeleeOffHandTpp.Get();
                }
                if (offHandAnimatorControllerRef?.IsSet ?? false) {
                    return offHandAnimatorControllerRef;
                }
                return _cachedOffHandAnimatorControllerRef ??= Services.Get<GameConstants>().defaultMeleeOffHand.Get();
            }
        }
        
        ARAsyncOperationHandle<FinishersList> _finisherListHandle, _executionListHandle;
        public FinishersList FinishersList => _finisherListHandle.IsValid() ? _finisherListHandle.Result : null;
        public FinishersList ExecutionsList => _executionListHandle.IsValid() ? _executionListHandle.Result : null;
        
        // === Weapon Collider
        public Transform ColliderPivot { get; private set; }
        Vector3 Size => Owner?.Character is Hero ? new Vector3(size.x, size.y, size.z + HeroLength) : GetNpcColliderSize();
        Vector3 GetNpcColliderSize() {
            if (Owner?.Character == null) {
                return size;
            }
            // --- Temporarily disabled since colliders are way to short for combat AI vs AI
            //var target = Owner.Character.GetCurrentTarget();
            //return target is Hero ? new Vector3(size.x, size.y, size.z + additionalLengthForNpcWhenFightingHero) : size;
            return new Vector3(size.x, size.y, size.z + additionalLengthForNpcWhenFightingHero);
        }
        float HeroLength {
            get {
                VHeroController heroController = Owner.View<VHeroController>();
                // --- normalize to -2 - 0
                float angle = heroController.FirePoint.transform.forward.y - 1;
                return additionalLengthForHero * angle * -1;
            }
        }
        ShareableARAssetReference Trail => (weaponTrailOverride?.IsSet ?? false) ? weaponTrailOverride : Services.Get<CommonReferences>().WeaponTrail;

        // === Initialization
        protected override void OnInitialize() {
            AsyncOnInitialize().Forget();
        }

        protected async UniTaskVoid AsyncOnInitialize() {
            _equippedInMainHand = Item.EquippedInSlotOfType == EquipmentSlotType.MainHand;
            base.OnInitialize();

            if (Owner?.Character != null) {
                AttachWeaponEventsListener();
                Owner.Character.ListenTo(IAlive.Events.BeforeDeath, OnDeath, this);
            }

            await InitializeColliderPivot();
            if (!this || HasBeenDiscarded) {
                return;
            }
            
            if (useWeaponTrail) {
                InstantiateWeaponTrail().Forget();
            }

            _attached = true;
        }

        /// <summary>
        /// When equipping fists they are instantly disabled, so they can't correctly setup their weapon colliders. That's why this OnEnable is here.
        /// </summary>
        protected override void OnEnable() {
            bool isFists = Item?.IsFists ?? false;
            if (isFists && ColliderPivot == null) {
                InitializeColliderPivot().Forget();
            }
            base.OnEnable();
        }

        async UniTask InitializeColliderPivot() {
            if (!await AsyncUtil.WaitWhile(gameObject, () => !UIStateStack.Instance.State.IsMapInteractive)) {
                return;
            }
            if (!this || HasBeenDiscarded) {
                return;
            }
            InitializeColliderPivotInternal();
        }

        void InitializeColliderPivotInternal() {
            ColliderPivot = new GameObject("Collider Pivot").transform;
            ColliderPivot.SetParent(colliderParent, false);
            Vector3 forward = colliderParent.forward;
            ColliderPivot.forward = forward * -1;
            ColliderPivot.localPosition = new Vector3(0, 0, Size.z);
            ColliderPivot.localEulerAngles = new Vector3(0, 180, 0);
        }

        // --- Instantiate weapon trail
        protected virtual async UniTaskVoid InstantiateWeaponTrail() {
            _spawnedTrail = await PrefabPool.Instantiate(Trail, Vector3.zero, Quaternion.identity, colliderParent);
            if (_spawnedTrail.Instance == null) {
                Log.Important?.Error($"Failed to initialize weapon trail for weapon: {this}");
                return;
            }
            if (!this || HasBeenDiscarded || Owner == null || Owner.HasBeenDiscarded) {
                _spawnedTrail.Return();
                return;
            }
            //_weaponTrail = _spawnedTrail.Instance.GetComponent<XWeaponTrail>();
            //_weaponTrail.PointEnd.localPosition = new Vector3(0, 0, size.z);
            //_weaponTrail.Init();
            // _weaponTrail.Deactivate();
            Owner.ListenTo(IItemOwner.Events.WeaponTrailChanged, RefreshTrailMaterial, this);
        }

        protected override void OnAttachedToHero(Hero hero) {
            _attachedToHero = true;

            base.OnAttachedToHero(hero);
            ARTimeUtils.SetAnimatorSpeed(HeroAnimancer.Animator, animatorSpeed);
            _listeners.ForEach(l => World.EventSystem.RemoveListener(l));
            _listeners.Clear();
            _listeners.Add(hero.ListenTo(ICharacter.Events.OnAttackRelease, OnAttackRelease, this));
            _listeners.Add(hero.ListenTo(ICharacter.Events.OnAttackRecovery, AttackEnded, this));
            _listeners.Add(hero.ListenTo(Hero.Events.ProcessAnimationSpeed, StartProcessingAnimationSpeed, this));
            _listeners.Add(hero.ListenTo(Hero.Events.StopProcessingAnimationSpeed, StopProcessingAnimationSpeed, this));

            LoadFinishers(Hero.TppActive ? finisherListTpp : finisherList, 
                Hero.TppActive ? executionListTpp : executionList);
        }
        
        void LoadFinishers(ARAssetReference finisherListRef, ARAssetReference executionListRef) {
            if (finisherListRef is { IsSet: true }) {
                _finisherListHandle = finisherListRef.LoadAsset<FinishersList>();
                _finisherListHandle.OnComplete(OnListLoaded);
            }

            if (executionListRef is { IsSet: true }) {
                _executionListHandle = executionListRef.LoadAsset<FinishersList>();
                _executionListHandle.OnComplete(OnListLoaded);
            }
            
            void OnListLoaded(ARAsyncOperationHandle<FinishersList> handle) {
                if (handle.Status == AsyncOperationStatus.Succeeded) {
                    handle.Result.Init();
                }
            }
        }

        protected override void OnAttachedToNpc(NpcElement npc) {
            _attachedToHero = false;
            
            _listeners.ForEach(l => World.EventSystem.RemoveListener(l));
            _listeners.Clear();
            _listeners.Add(npc.ListenTo(ICharacter.Events.OnAttackRelease, OnAttackRelease, this));
            _listeners.Add( npc.ListenTo(ICharacter.Events.OnAttackRecovery, AttackEnded, this));
            _listeners.Add(npc.ListenTo(EnemyBaseClass.Events.AttackInterrupted, OnAttackInterrupt, this));
            _listeners.Add(npc.ListenTo(ICharacter.Events.CombatExited, AttackEnded, this));
        }
        
        void OnAttackRelease(ARAnimationEventData eventData) {
            bool match = Owner is Hero ? _currentRestriction.Match(this) : eventData.restriction.Match(this);
            if (match) {
                AttackBegun(eventData.attackType);
            } else {
                AttackEnded();
            }
        }
        
        void AttackBegun(AttackType attackType) {
            if (!_attached) {
                return;
            }

            if (Owner is Hero {IsAnimatorInAttackState: false} and {IsInToolAnimation: false}) {
                return;
            }

            if (Owner is Location loc && loc.TryGetElement<EnemyBaseClass>()?.CurrentBehaviour.Get() is IInterruptBehaviour) {
                return;
            }

            if (Owner?.Character != null) {
                Owner.Character.CharacterDealingDamage.OnAttackBegun();
                _currentAttackParameters = new AttackParameters(Owner.Character, Item, attackType, null);
                Owner.Character.Trigger(ICharacter.Events.OnAttackStart, _currentAttackParameters);
                
                if (Owner.Character is NpcElement npcElement) {
                    NpcAngularSpeedMultiplier.AddAngularSpeedMultiplier(npcElement, angularSpeedModifierWhenAttacking,
                        new UntilEndOfAttack(npcElement, AngularSpeedModifierDurationExtend));
                }
            }

            _attackType = attackType;
            AttachUpdates();

            // if (_weaponTrail != null) {
            //     _weaponTrail.Activate();
            // }

            _inAttack = true;
        }

        void StartProcessingAnimationSpeed(AnimationSpeedParams animationSpeedParams) {
            _currentRestriction = animationSpeedParams.Restriction;
            // --- If this attack doesn't belong to us, ignore.
            if (!animationSpeedParams.Restriction.Match(this)) {
                return;
            }
            _currentAnimLayer = animationSpeedParams.Layer;
            _speedMultiplyCurve = animationSpeedParams.SpeedMultiplyCurve;
            _speedMultiplyCurve ??= CommonReferences.Get.DefaultWeaponCurve(Item);
            _attackType = animationSpeedParams.IsHeavy ? AttackType.Heavy : AttackType.Normal;
            Target.GetOrCreateTimeDependent()?.WithUpdate(ProcessAnimationSpeed);
        }

        void StopProcessingAnimationSpeed() {
            Target.GetTimeDependent()?.WithoutUpdate(ProcessAnimationSpeed);
            AfterStoppedProcessingAnimationSpeed();
        }

        protected virtual void AfterStoppedProcessingAnimationSpeed() {
            EndAttack();
            TriggerAttackEnded();
        }

        void AttackEnded() {
            if (!_attached) {
                return;
            }
            TriggerAttackEnded();
            EndAttack();
        }

        void TriggerAttackEnded() {
            if (_inAttack) {
                var attackParameters = new AttackParameters(Owner.Character, Item, _attackType, _moveDirection);
                HandOwner?.OnAttackEnded(attackParameters);
            }
            _inAttack = false;
        }

        void EndAttack() {
            DetachUpdates();
            
            // if (_weaponTrail != null) {
            //     _weaponTrail.StopSmoothly(0.25f);
            // }
        }

        void ProcessUpdate(float deltaTime) {
            _moveDirection = ColliderPivot.position - _lastUpdatePosition;
            Vector3 tempSize = Size;
            float divider = HandOwner.WeaponColliderDivider;
            LayerMask layerMask = HandOwner.HitLayerMask;
            int points = Mathf.CeilToInt(_moveDirection.magnitude / (tempSize.x / divider));
            points = points < 2 ? 2 : points;
            points = Mathf.Min(points, 100);
            float moveDirStep = 1 / (float) (points - 1);
            for (int i = 0; i < points; i++) {
                float currentStep = i > 0 ? moveDirStep * i : 0;

                Vector3 position = _lastUpdatePosition + _moveDirection * currentStep;
                Vector3 center = position;
                Vector3 forward = Vector3.Lerp(_lastUpdateForward, ColliderPivot.forward, currentStep);
                center += forward * tempSize.z / 2f;
                Quaternion rotation = Quaternion.Slerp(_lastUpdateRotation, ColliderPivot.rotation, currentStep);
                
                int hits = Physics.BoxCastNonAlloc(center, tempSize / 2f, ColliderPivot.forward, _hitResults, rotation, RaycastCheck.MinPhysicsCastDistance, layerMask);
                Vector3 colliderPivotPosition = position + forward * tempSize.z;
                for (int j = 0; j < hits; j++) {
                    Vector3 hitPoint = _hitResults[j].collider.ClosestPoint(center);
                    _hitResults[j].point = hitPoint;
                    bool inEnviroHitRange = false;
                    if (_attachedToHero) {
                        Ray ray = new(colliderPivotPosition, hitPoint - colliderPivotPosition);
                        int enviroHits = Physics.RaycastNonAlloc(ray, _enviroHits, tempSize.z * EnviroHitRange, layerMask);
                        inEnviroHitRange = enviroHits > 0;
                    }

                    _currentAttackParameters.AttackDirection = _moveDirection.normalized;
                    OnBoxCastHit(_hitResults[j], inEnviroHitRange, in _currentAttackParameters);
                }
#if UNITY_EDITOR
                var matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
                _cubesToDraw.Add(new CubeToDraw(matrix, Time.frameCount));
#endif
            }

            _lastUpdatePosition = ColliderPivot.position;
            _lastUpdateForward = ColliderPivot.forward;
            _lastUpdateRotation = ColliderPivot.rotation;
        }

        // --- animation speed
        void ProcessAnimationSpeed(float deltaTime) {
            if (_speedMultiplyCurve != null && Owner?.Character != null) {
                float animationNormalizedTime = _currentAnimLayer.CurrentState?.NormalizedTime ?? 0;
                float curveMultiplier = _speedMultiplyCurve.Evaluate(animationNormalizedTime);
                if (curveMultiplier >= 0) {
                    bool isHeavy = _attackType is AttackType.Heavy;
                    var characterStat = Owner.Character.CharacterStats.SelectAttackSpeed(Item, isHeavy);
                    var attackSpeedStat = Owner.Character.CharacterStats.AttackSpeed;
                    
                    float baseValue = isHeavy ? 0.9f : 1f;
                    float optionalItemStatMultiplier = Item.ItemStats.GetAttackSpeedMultiplierItemDependent()?.ModifiedValue ?? 1;
                    float statResultantSpeed = M.MergeMultipliers(
                        characterStat.ModifiedValue * optionalItemStatMultiplier, 
                        attackSpeedStat.ModifiedValue);
                    
                    float attackSpeedCapped = Mathf.Min(characterStat.UpperLimit, statResultantSpeed);
                    float modifier = Mathf.Max(0.1f, baseValue + (attackSpeedCapped - baseValue) * curveMultiplier);
                    
                    AnimancerAttackSpeed animatorParameter = SelectAnimatorParameter();
                    animatorParameter.SetAttackSpeed.Invoke(HeroAnimancer, modifier);
                    AfterAnimationSpeedProcessed(animatorParameter.AnimatorHash, modifier);
                }
            }
        }

        protected virtual void AfterAnimationSpeedProcessed(int parameterHash, float modifier) {}

        protected virtual void OnBoxCastHit(in RaycastHit other, bool inEnviroHitRange, in AttackParameters attackParameters) {
            HandOwner?.OnWeaponTriggerEnter(other, attackParameters, inEnviroHitRange);
        }

        // === Helpers
        void OnAttackInterrupt(bool triggerHandOwnerEvents) {
            if (triggerHandOwnerEvents) {
                AttackEnded();
            } else {
                EndAttack();
            }
        }
        
        // TODO: This method should have some sort of PriorityQueue and could have many overrides added at the same time (and active one with highest priority)
        void RefreshTrailMaterial(Material material) {
            // if (_weaponTrail != null) {
            //     _weaponTrail.ChangeMaterial(material);
            // }
        }
        
        void OnDeath() {
            if (HasBeenDiscarded) {
                return;
            }
            DetachUpdates();
        }

        public override void HideWeapon(bool instantHide) {
            DetachUpdates();
            base.HideWeapon(instantHide);
        }

        protected override IBackgroundTask OnDiscard() {
            DetachUpdates();
            _spawnedTrail?.Return();
            if (_finisherListHandle.IsValid()) {
                if (_finisherListHandle.Result != null) {
                    _finisherListHandle.Result.Unload();
                }
                _finisherListHandle.Release();
            }

            return base.OnDiscard();
        }

        void DetachUpdates() {
            Target.GetTimeDependent()?.WithoutUpdate(ProcessAnimationSpeed);
            if (_attached) {
                Target.GetTimeDependent()?.WithoutLateUpdate(ProcessUpdate);
            }
#if UNITY_EDITOR
            _cubesToDraw.Clear();
#endif
        }

        void AttachUpdates() {
            if (colliderParent == null || ColliderPivot == null) {
                Log.Important?.Error($"Attach Updates invoked event though weapon collider is still uninitialized! {this}");
                return;
            }
            
            // --- Update collider pivot position since Size.z depends from enemy
            ColliderPivot.position = colliderParent.position + colliderParent.forward * Size.z;
            _lastUpdatePosition = ColliderPivot.position;
            _lastUpdateRotation = ColliderPivot.rotation;
            _lastUpdateForward = ColliderPivot.forward;
            Target.GetOrCreateTimeDependent().WithoutLateUpdate(ProcessUpdate).WithLateUpdate(ProcessUpdate);
            
#if UNITY_EDITOR
            _cubesToDraw.Clear();
#endif
        }

#if UNITY_EDITOR
        readonly struct CubeToDraw {
            public readonly Matrix4x4 matrix;
            public readonly int frame;
            public CubeToDraw(Matrix4x4 matrix, int frame) {
                this.matrix = matrix;
                this.frame = frame;
            }
        }
        readonly List<CubeToDraw> _cubesToDraw = new();

        void OnDrawGizmosSelected() {
            if (colliderParent == null) {
                CreateColliderParent();
            }

            Gizmos.matrix = Matrix4x4.TRS(colliderParent.position, colliderParent.rotation, Vector3.one);
            Gizmos.color = Color.green;
            float z = size.z + additionalLengthForHero;
            Gizmos.DrawWireCube(new Vector3(0, 0, z / 2f), new Vector3(size.x, size.y, z));
            Gizmos.color = Color.blue;
            float npcZ = size.z + additionalLengthForNpcWhenFightingHero;
            Gizmos.DrawWireCube(new Vector3(0, 0, npcZ / 2f), new Vector3(size.x, size.y, npcZ));
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(0, 0, size.z / 2f), size);
            Gizmos.color = Color.white;
        }

        void OnDrawGizmos() {
            if (Selection.objects.Contains(colliderParent.gameObject)) {
                OnDrawGizmosSelected();
            }

            foreach (var cubeToDraw in _cubesToDraw) {
                Gizmos.matrix = cubeToDraw.matrix;
                float lerp = (Time.frameCount - cubeToDraw.frame) / 10f;
                Gizmos.color = Color.Lerp(Color.red, Color.cyan, lerp);
                Gizmos.DrawWireCube(Vector3.zero, Size);
            }
        }

        void OnValidate() {
            if (colliderParent == null) {
                CreateColliderParent();
            }
            if (size == Vector3.zero) {
                RecalculateBounds();
            }
        }
#endif

        void CreateColliderParent() {
            GameObject go = new("Weapon Collider Parent");
            go.transform.SetParent(transform);
            colliderParent = go.transform;
            colliderParent.localPosition = Vector3.zero;
            colliderParent.localRotation= Quaternion.identity;
        }

        [Button, FoldoutGroup("Collider Settings")]
        void RecalculateBounds() {
            Bounds bounds = TransformBoundsUtil.FindBounds(transform, false);
            size = bounds.size;
        }

        AnimancerAttackSpeed SelectAnimatorParameter() =>
            (_attackType is AttackType.Heavy) switch {
                _ when Item.IsRanged => AnimancerAttackSpeed.BowDrawSpeed,
                true when Item.IsFists || Item.IsTwoHanded => AnimancerAttackSpeed.HeavyAttackMult2H,
                true when Item.IsOneHanded => AnimancerAttackSpeed.HeavyAttackMult1H,
                false when Item.IsFists || Item.IsTwoHanded => AnimancerAttackSpeed.LightAttackMult2H,
                false when Item.IsOneHanded => AnimancerAttackSpeed.LightAttackMult1H,
                true => AnimancerAttackSpeed.HeavyAttackMult1H,
                false => AnimancerAttackSpeed.LightAttackMult1H
            };
    }
}
