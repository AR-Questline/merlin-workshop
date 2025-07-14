using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.AI.Fights.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemProjectile : Element<Item>, IRefreshedByAttachment<ItemProjectileAttachment>, ISkillOwner, ISkillProvider {
        public override ushort TypeForSerialization => SavedModels.ItemProjectile;

        ItemProjectileAttachment _spec;
        
        public static ShareableARAssetReference DefaultVisualRef => World.Services.Get<CommonReferences>().ArrowPrefab;
        public static ShareableARAssetReference DefaultLogicRef => World.Services.Get<CommonReferences>().ArrowLogicPrefab;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public ProjectileData Data => _spec.data.ToProjectileData();
        
        ShareableARAssetReference VisualRef => _spec.data.visualPrefab;
        ShareableARAssetReference LogicRef => _spec.data.logicPrefab;
        IEnumerable<SkillReference> SkillRefs => _spec.data.skills;
        ProjectileLogicData LogicData => _spec.data.logicData;

        public IEnumerable<Skill> Skills => Elements<Skill>().GetManagedEnumerator();
        public ICharacter Character => null;

        public void InitFromAttachment(ItemProjectileAttachment spec, bool isRestored) {
            _spec = spec;
            foreach (var skillRef in _spec.Skills) {
                // Skills are not saved (see AllowElementSave)
                var skill = skillRef.CreateSkill();
                AddElement(skill);
            }
        }
        
        public override bool AllowElementSave(Element ele) {
            return false;
        }
        
        // === Visual Projectile

        // From Item
        public async UniTask<IPooledInstance> GetVisualProjectile(Transform parent, CancellationTokenSource tokenSource, bool startAutomatically = false) {
            return await PrepareVisualProjectile(VisualRef, parent, tokenSource, false, startAutomatically);
        }
        // Default
        public static async UniTask<IPooledInstance> GetDefaultVisualProjectile(Transform parent, CancellationTokenSource tokenSource, bool startAutomatically = false) {
            return await PrepareVisualProjectile(DefaultVisualRef, parent, tokenSource, false, startAutomatically);
        }
        // Custom
        public static async UniTask<IPooledInstance> GetCustomVisualProjectile(ShareableARAssetReference assetReference, Transform parent, CancellationTokenSource tokenSource, bool startAutomatically = false) {
            return await PrepareVisualProjectile(assetReference, parent, tokenSource, false, startAutomatically);
        }
        
        
        // === In Hand Projectile
        
        // From Item
        public async UniTask<IPooledInstance> GetInHandProjectile(Transform parent, CancellationTokenSource tokenSource, bool disableShadowCasting = true) {
            return await PrepareVisualProjectile(VisualRef, parent, tokenSource, disableShadowCasting);
        }
        
        // Default
        public static async UniTask<IPooledInstance> GetDefaultInHandProjectile(Transform parent, CancellationTokenSource tokenSource, bool disableShadowCasting = true) {
            return await PrepareVisualProjectile(DefaultVisualRef, parent, tokenSource, disableShadowCasting);
        }
        
        // Custom
        public static async UniTask<IPooledInstance> GetCustomInHandProjectile(ShareableARAssetReference assetReference, Transform parent, CancellationTokenSource tokenSource, bool disableShadowCasting = true) {
            return await PrepareVisualProjectile(assetReference, parent, tokenSource, disableShadowCasting);
        }
        
        
        static async UniTask<IPooledInstance> PrepareVisualProjectile(
            ShareableARAssetReference assetReference, Transform parent, CancellationTokenSource tokenSource, 
            bool disableShadowCasting = true, bool startAutomatically = true) {
            
            var cancellationToken = tokenSource?.Token ?? default;
            IPooledInstance instance = await PrefabPool.Instantiate(assetReference, Vector3.zero, Quaternion.identity, parent, Vector3.one, 
                cancellationToken, startAutomatically);

            if (instance.Instance != null) {
                instance.Instance.GetComponentsInChildren<TrailRenderer>().ForEach(t => t.Clear());
                var visualData = instance.Instance.GetComponentInChildren<ProjectileVisualData>();
                if (visualData != null && visualData.trailHolder != null) {
                    visualData.trailHolder.SetActive(false);
                }
                if (disableShadowCasting) {
                    instance.Instance.GetComponentsInChildren<MeshRenderer>().ForEach(m =>
                        m.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off);
                }
            }

            return instance;
        }

        // === Preload
        // From Item
        public ProjectilePreload PreloadProjectile() {
            var refs = new ProjectilePreload(LogicRef.Get(), VisualRef.Get());
            refs.Preload();
            return refs;
        }
        
        // Default
        public static ProjectilePreload PreloadDefaultProjectile() {
            var refs = new ProjectilePreload(DefaultLogicRef.Get(), DefaultVisualRef.Get());
            refs.Preload();
            return refs;
        }
        
        // Custom
        public static ProjectilePreload PreloadCustomProjectile(ARAssetReference logicRef, ShareableARAssetReference visualRef) {
            var refs = new ProjectilePreload(logicRef, visualRef.Get());
            refs.Preload();
            return refs;
        }

        // === Real Projectile
        // From Item
        public async UniTask<CombinedProjectile> GetProjectile(bool releaseOnDestroy, [CanBeNull] Transform followTransformWhileLoading, Transform firePoint, CancellationTokenSource tokenSource) {
            return await GetProjectile(firePoint.position, firePoint.rotation, releaseOnDestroy, followTransformWhileLoading, firePoint, tokenSource);
        }
        public async UniTask<CombinedProjectile> GetProjectile(Vector3 pos, Quaternion rot, bool releaseOnDestroy, [CanBeNull] Transform followTransformWhileLoading, [CanBeNull] Transform firePoint, CancellationTokenSource tokenSource) {
            var afterLoadTransformData = new AfterLoadTransformData(followTransformWhileLoading);
            var logicRef = LogicRef.Get();
            var logicPrefab = GetLogicPrefab(logicRef, pos, rot);
            var inHand = GetVisualProjectile(null, tokenSource);
            var creationData = new ProjectileData(LogicRef, VisualRef, SkillRefs, LogicData);
            return await CombinePrefabs(logicRef, logicPrefab, inHand, afterLoadTransformData, firePoint, releaseOnDestroy, creationData);
        }

        static async UniTask<GameObject> GetLogicPrefab(ARAssetReference logicRef, Vector3 pos, Quaternion rot) {
            var go = await PrefabUtil.InstantiateAsync(logicRef, pos, rot);
            if (go != null) {
                var rb = go.GetComponentInChildren<Rigidbody>();
                rb.isKinematic = true;
            }
            return go;
        }

        // Default
        public static async UniTask<CombinedProjectile> GetDefaultArrow(bool releaseOnDestroy, [CanBeNull] Transform followTransformWhileLoading, Transform firePoint, CancellationTokenSource tokenSource) {
            return await GetDefaultArrow(firePoint.position, firePoint.rotation, releaseOnDestroy, followTransformWhileLoading, firePoint, tokenSource);
        }
        
        public static async UniTask<CombinedProjectile> GetDefaultArrow(Vector3 pos, Quaternion rot, bool releaseOnDestroy, [CanBeNull] Transform followTransformWhileLoading, [CanBeNull] Transform firePoint, CancellationTokenSource tokenSource) {
            var afterLoadTransformData = new AfterLoadTransformData(followTransformWhileLoading);
            var logicPrefab = GetDefaultLogicPrefab(pos, rot, out var logicRef);
            var inHand = GetDefaultVisualProjectile(null, tokenSource);
            var creationData = new ProjectileData(DefaultLogicRef, DefaultVisualRef, null, ProjectileLogicData.Default);
            return await CombinePrefabs(logicRef, logicPrefab, inHand, afterLoadTransformData, firePoint, releaseOnDestroy, creationData);
        }

        static UniTask<GameObject> GetDefaultLogicPrefab(Vector3 pos, Quaternion rot, out ARAssetReference logicRef) {
            logicRef = DefaultLogicRef.Get();
            return PrefabUtil.InstantiateAsync(logicRef, pos, rot);
        }
        
        // Custom
        public static async UniTask<CombinedProjectile> GetCustomProjectile(ProjectileData data, Vector3 pos, Quaternion rot, bool releaseOnDestroy, [CanBeNull] Transform followTransformWhileLoading, [CanBeNull] Transform firePoint, CancellationTokenSource tokenSource) {
            var afterLoadTransformData = new AfterLoadTransformData(followTransformWhileLoading);
            var logicPrefabRef = data.logicPrefab.Get();
            var logicPrefab = PrefabUtil.InstantiateAsync(logicPrefabRef, pos, rot);
            var inHand = GetCustomVisualProjectile(data.visualPrefab, null, tokenSource);
            return await CombinePrefabs(logicPrefabRef, logicPrefab, inHand, afterLoadTransformData, firePoint, releaseOnDestroy, data);
        }

        [UnityEngine.Scripting.Preserve]
        public static async UniTask<CombinedProjectile> GetCustomProjectile(ShareableARAssetReference logicRef, ShareableARAssetReference visualRef, ProjectileLogicData? logicData, IEnumerable<SkillReference> skillReferences, Vector3 pos, Quaternion rot, 
            [CanBeNull] Transform followTransformWhileLoading, [CanBeNull] Transform firePoint, bool releaseOnDestroy, CancellationTokenSource tokenSource) {
            var data = new ProjectileData(logicRef, visualRef, skillReferences, logicData ?? ProjectileLogicData.Default);
            return await GetCustomProjectile(data, pos, rot, releaseOnDestroy, followTransformWhileLoading, firePoint, tokenSource);
        }

        /// <summary>
        /// Combines visual prefab and logic prefab to avoid unnecessary prefabs duplications in project
        /// </summary>
        public static async UniTask<CombinedProjectile> CombinePrefabs(ARAssetReference logicPrefabReference, UniTask<GameObject> logicPrefabUniTask, UniTask<IPooledInstance> inHandUniTask, 
            AfterLoadTransformData afterLoadTransformData, [CanBeNull] Transform firePoint, bool releaseOnDestroy, ProjectileData creationData) {
            (GameObject logicPrefab, IPooledInstance inHand) = await UniTask.WhenAll(logicPrefabUniTask, inHandUniTask);
            
            if (inHand.Instance != null && logicPrefab != null) {
                var projectile = logicPrefab.GetComponentInChildren<Projectile>();
                if (projectile == null) {
                    inHand.Return();
                    logicPrefabReference?.ReleaseAsset();
                    return new CombinedProjectile(null, inHand);
                }
                logicPrefab.AddComponent<OnDestroyReleaseAsset>().Init(logicPrefabReference);
                // Configure transform
                inHand.Instance.SetActive(true);
                var inHandTransform = inHand.Instance.transform;
                var parent = projectile?.VisualParent;
                if (parent == null) {
                    parent = logicPrefab.transform;
                }
                inHandTransform.SetParent(parent);
                inHandTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                
                await AsyncUtil.WaitForEndOfFrame(projectile);
                afterLoadTransformData.GetMoveOffset(out var positionOffset, out var rotationOffset);
                logicPrefab.transform.position += positionOffset;
                logicPrefab.transform.rotation *= rotationOffset;
                
                // Cleanup
                logicPrefab.GetComponentsInChildren<TrailRenderer>().ForEach(t => t.Clear());
                inHandTransform.GetComponentsInChildren<Collider>().ForEach(c => c.enabled = false);
                if (releaseOnDestroy) {
                    projectile?.ReleaseAddressablesInstanceOnDestroy(inHand);
                }
                
                // Apply Skills
                var projectileVisualData = inHandTransform.GetComponent<ProjectileVisualData>();
                projectile.Setup(creationData.logicData, projectileVisualData, creationData.skills, firePoint, creationData);
            } else {
                inHand?.Return();
                logicPrefabReference?.ReleaseAsset();
            }
            return new CombinedProjectile(logicPrefab, inHand);
        }
    }

    public struct CombinedProjectile {
        public GameObject logic;
        public IPooledInstance visual;
        
        public CombinedProjectile(GameObject logic, IPooledInstance visual) {
            this.logic = logic;
            this.visual = visual;
        }
    }

    public struct ProjectilePreload {
        ARAssetReference _logicRef;
        ARAssetReference _visualRef;
        AsyncOperationHandle<GameObject> _logicPreload;
        AsyncOperationHandle<GameObject> _visualPreload;
        
        public ProjectilePreload(ARAssetReference logicRef, ARAssetReference visualRef) {
            this._logicRef = logicRef;
            this._visualRef = visualRef;
            this._logicPreload = default;
            this._visualPreload = default;
        }
        
        public void Preload() {
            if (_logicPreload.IsValid() == false) {
                _logicPreload = _logicRef.PreloadLight<GameObject>();
            }

            if (_visualPreload.IsValid() == false) {
                _visualPreload = _visualRef.PreloadLight<GameObject>();
            }
        }

        public void Release() {
            if (_logicPreload.IsValid()) {
                _logicRef.ReleasePreloadLight(_logicPreload);
                _logicPreload = default;
            }

            if (_visualPreload.IsValid()) {
                _visualRef.ReleasePreloadLight(_visualPreload);
                _visualPreload = default;
            }
            
            _logicRef = null;
            _visualRef = null;
        }
    }

    public struct AfterLoadTransformData { 
        readonly Vector3 _cachedPosition;
        readonly Quaternion _cachedRotation;
        readonly Transform _shooter;

        public AfterLoadTransformData(Transform shooter) {
            this._shooter = shooter;
            if (shooter == null) {
                _cachedPosition = Vector3.zero;
                _cachedRotation = Quaternion.identity;
                return;
            }
            _cachedPosition = shooter.position;
            _cachedRotation = shooter.rotation;
        }

        public void GetMoveOffset(out Vector3 positionOffset, out Quaternion rotationOffset) {
            if (_shooter == null) {
                positionOffset = Vector3.zero;
                rotationOffset = Quaternion.identity;
                return;
            }
            positionOffset = _shooter.position - _cachedPosition;
            rotationOffset = Quaternion.Inverse(_cachedRotation) * _shooter.rotation;
        }
    }
}