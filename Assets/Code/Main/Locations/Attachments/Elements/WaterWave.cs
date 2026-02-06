using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class WaterWave : Element<Location>, IRefreshedByAttachment<WaterWaveAttachment>, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.WaterWave;
        public static ShareableARAssetReference ComputeShaderAddressable => GameConstants.Get.waterWaveComputeShader;
        const float SafetyMargin = 1.66f;
        const int PixelsPerMeter = 10;
        
        WaterWaveAttachment _spec;
        StructList<WeakModelRef<Location>> _blockers;
        [Saved] float _cycleTimer;
        RenderTexture _maskTexture;
        ARAsyncOperationHandle<ComputeShader> _computeShaderHandle;
        
        public void InitFromAttachment(WaterWaveAttachment spec, bool isRestored) {
            _spec = spec;
        }
        
        protected override void OnInitialize() {
            _cycleTimer = _spec.cycleDuration - _spec.cycleInitialDelay;
            _computeShaderHandle = ComputeShaderAddressable.Get().LoadAsset<ComputeShader>();
            World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
        }

        protected override void OnRestore() {
            _computeShaderHandle = ComputeShaderAddressable.Get().LoadAsset<ComputeShader>();
            World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
        }

        public void UnityUpdate() {
            _cycleTimer += Time.deltaTime;
            if (_cycleTimer >= _spec.cycleDuration) {
                _cycleTimer -= _spec.cycleDuration;
                Trigger();
            }
        }

        void Trigger() {
            if (_blockers is not { IsCreated: true }) {
                _spec.GetBlockerLocations(ref _blockers);
            }
            if (_computeShaderHandle.Status == AsyncOperationStatus.None) {
                _computeShaderHandle.OnComplete(_ => Trigger());
                return;
            }
            var blockerPositions = GetAllBlockerPositions(ref _blockers);
            ComputeMaskTexture(ParentModel.Coords, blockerPositions, _spec.blockerRadius, _spec.blockerDistance, _spec.waveMaskTextureHalfSizeInMeters, _computeShaderHandle.Result, ref _maskTexture);
            
            var parameters = _spec.GetParameters();
            if (_spec.damageAngle < 360) {
                var coneParameters = new ConeDamageParameters() {
                    angle = _spec.damageAngle,
                    forward = ParentModel.Forward(),
                    sphereDamageParameters = parameters
                };
                TriggerWaterWave(ParentModel, null, coneParameters, blockerPositions, _spec.blockerRadius, _spec.blockerDistance,
                    _spec.waveVFX, _spec.waveVFXLifetime, ref _maskTexture);
            } else {
                TriggerWaterWave(ParentModel, null, parameters, blockerPositions, _spec.blockerRadius, _spec.blockerDistance, 
                    _spec.waveVFX, _spec.waveVFXLifetime, ref _maskTexture);
            }
            SpawnVfxAndDestroyBlockers(ParentModel, ref _blockers, _spec.waveHittingBlockerVFX, ParentModel.Coords.ToVector2(), parameters.endRadius / parameters.duration, false, 0);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Services.TryGet<UnityUpdateProvider>()?.UnregisterGeneric(this);
            if (_maskTexture != null) {
                _maskTexture.Release();
                Object.Destroy(_maskTexture);
                _maskTexture = null;
            }
            if (_computeShaderHandle.IsValid()) {
                _computeShaderHandle.Release();
                _computeShaderHandle = default;
            }
        }

        public static void TriggerWaterWave(IGrounded owner, [CanBeNull] ICharacter attacker, SphereDamageParameters parameters, 
            Vector2[] blockerPositions, float blockerRadius, float blockerDistance, 
            ShareableARAssetReference waveVfx, float waveVfxLifetime, ref RenderTexture renderTexture) {
            var position = owner.Coords;
            var v2Position = position.ToVector2();
            owner.AddElement(new DealDamageInSphereOverTimeWithExternalChecks(parameters, position, attacker, coll => IsNoBlockerProtecting(coll, v2Position, blockerPositions, blockerRadius, blockerDistance)));
            SpawnWaveVFX(waveVfx, position, owner.Rotation, waveVfxLifetime, renderTexture).Forget();
        }

        public static void TriggerWaterWave(IGrounded owner, [CanBeNull] ICharacter attacker, ConeDamageParameters parameters,
            Vector2[] blockerPositions, float blockerRadius, float blockerDistance,
            ShareableARAssetReference waveVfx, float waveVfxLifetime, ref RenderTexture renderTexture) {
            var position = owner.Coords;
            var v2Position = position.ToVector2();
            owner.AddElement(new DealDamageInConeOverTimeWithExternalChecks(parameters, position, attacker, coll => IsNoBlockerProtecting(coll, v2Position, blockerPositions, blockerRadius, blockerDistance)));
            SpawnWaveVFX(waveVfx, position, owner.Rotation, waveVfxLifetime, renderTexture).Forget();
        }

        static async UniTaskVoid SpawnWaveVFX(ShareableARAssetReference waveVfx, Vector3 position, Quaternion rotation, float waveVfxLifetime,
            RenderTexture maskTexture = null) {
            var instance = await PrefabPool.InstantiateAndReturn(waveVfx, position, rotation, waveVfxLifetime);
            if (instance.Instance == null || !instance.Instance.TryGetComponent(out VisualEffect vfx)) {
                return;
            }
            if (maskTexture != null) {
                vfx.SetTexture("BlockerMask", maskTexture);
            }
        }

        public static void ComputeMaskTexture(Vector3 position, Vector2[] blockerPositions, float blockerRadius, float blockerDistance, int targetTextureSize, ComputeShader computeShader, ref RenderTexture maskTexture) {
            // Calculate texture size
            var positionXZ = position.XZ();
            int texWidth = 0;
            int texHeight = 0;
            for (int i = 0; i < blockerPositions.Length; i++) {
                texWidth = math.max(texWidth, (int) math.abs(blockerPositions[i].x - position.x));
                texHeight = math.max(texHeight, (int) math.abs(blockerPositions[i].y - position.z));
            }
            texWidth += (int) (SafetyMargin * blockerDistance);
            texHeight += (int) (SafetyMargin * blockerDistance);
            
            Vector2 leftUpperWorldPos = positionXZ - new Vector2(texWidth, texHeight);
            texWidth *= 2 * PixelsPerMeter;
            texHeight *= 2 * PixelsPerMeter;    
            
            targetTextureSize = ((2 * PixelsPerMeter * targetTextureSize + 7) / 8) * 8;
            texWidth = math.min(texWidth, targetTextureSize);
            texHeight = math.min(texHeight, targetTextureSize);
            
            // Create texture
            if (maskTexture != null && maskTexture.width != targetTextureSize) {
                maskTexture.Release();
                Object.Destroy(maskTexture);
                maskTexture = null;
            }
            if (maskTexture == null) {
                maskTexture = new RenderTexture(targetTextureSize, targetTextureSize, 0, RenderTextureFormat.ARGB32);
                maskTexture.enableRandomWrite = true;
                maskTexture.Create();
            }

            // Setup
            var pointBuffer = new ComputeBuffer(blockerPositions.Length, sizeof(float) * 2);
            pointBuffer.SetData(blockerPositions);
            int kernel = computeShader.FindKernel("Main");
            computeShader.SetInt("Width", texWidth);
            computeShader.SetInt("Height", texHeight);
            computeShader.SetInt("TextureSize", targetTextureSize);
            computeShader.SetInt("PointCount", blockerPositions.Length);
            computeShader.SetFloat("PointRadius", blockerRadius);
            computeShader.SetFloat("MaxDistance", blockerDistance);
            computeShader.SetVector("CenterPos", positionXZ);
            computeShader.SetVector("LeftUpperWorldPos", leftUpperWorldPos);
            computeShader.SetFloat("PixelPerMeterDivide", 1f / PixelsPerMeter);
            computeShader.SetTexture(kernel, "Result", maskTexture);
            computeShader.SetBuffer(kernel, "Points", pointBuffer);

            // Compute
            computeShader.Dispatch(kernel, Mathf.CeilToInt(targetTextureSize / 8f), Mathf.CeilToInt(targetTextureSize / 8f), 1);

            // Cleanup
            pointBuffer.Release();
        }

        public static void SpawnVfxAndDestroyBlockers(IModel owner, ref StructList<WeakModelRef<Location>> blockers, ShareableARAssetReference vfx, Vector2 position, float waveSpeed, bool destroyBlockers, float destroyDelay) {
            for (int i = 0; i < blockers.Count; i++) {
                if (!blockers[i].TryGet(out var blocker)) {
                    continue;
                }
                
                var dist2D = position - blocker.Coords.ToVector2();
                var rot = Quaternion.LookRotation(dist2D.X0Y());
                float delay = dist2D.magnitude / waveSpeed;
                SpawnVfxAndDestroyBlocker(owner, blocker, vfx, blocker.Coords, rot, delay, destroyBlockers, destroyDelay).Forget();
            }
            if (destroyBlockers) {
                blockers.Clear();
            }
        }

        static async UniTaskVoid SpawnVfxAndDestroyBlocker(IModel owner, Location blocker, ShareableARAssetReference vfx, Vector3 pos, Quaternion rot, float delay,
            bool destroyBlocker, float destroyDelay) {
            if (!await AsyncUtil.DelayTime(owner, delay)) {
                return;
            }
            await PrefabPool.InstantiateAndReturn(vfx, pos, rot);
            if (owner.HasBeenDiscarded) {
                return;
            }
            if (!destroyBlocker) {
                return;
            }
            
            if (!await AsyncUtil.DelayTime(owner, destroyDelay)) {
                return;
            }
            if (blocker is { HasBeenDiscarded: false }) {
                blocker.Discard();
            }
        }

        public static Vector2[] GetAllBlockerPositions(ref StructList<WeakModelRef<Location>> blockers) {
            var positions = new Vector2[blockers.Count];
            for (int i = 0; i < blockers.Count; i++) {
                if (!blockers[i].TryGet(out var blocker)) {
                    positions[i] = Vector2.negativeInfinity;
                    continue;
                }
                positions[i] = blocker.Coords.ToVector2();
            }
            return positions;
        }
        
        static bool IsNoBlockerProtecting(Collider collider, Vector2 npcPos, Vector2[] blockerPoints, float radius, float maxDistance) {
            var targetPos = collider.transform.position.ToVector2();
            return IsNoBlockerProtecting(targetPos, npcPos, blockerPoints, radius, maxDistance);
        }
        
        static bool IsNoBlockerProtecting(Vector2 testPos, Vector2 npcPos, Vector2[] blockerPoints, float radius, float maxDistance) {
            var toTarget = testPos - npcPos;
            var radiusSqr = radius * radius;
            for (int i = 0; i < blockerPoints.Length; i++) {
                if (blockerPoints[i] == Vector2.negativeInfinity) {
                    continue;
                }
                var toBlockerCenter = blockerPoints[i] - npcPos;
                if (toBlockerCenter.sqrMagnitude < radiusSqr) {
                    return false;
                }
                
                float t = Vector2.Dot(toBlockerCenter, toTarget) / toTarget.sqrMagnitude;
                t = math.clamp(t, 0, 1);

                Vector2 closestPoint = npcPos + t * toTarget;
                bool isInLineWithBlocker = (blockerPoints[i] - closestPoint).sqrMagnitude <= radiusSqr;
                if (!isInLineWithBlocker) {
                    continue;
                }
                
                float distanceToBlocker = Vector2.Distance(blockerPoints[i], npcPos);
                float distanceToTarget = Vector2.Distance(testPos, npcPos);
                bool inCorrectDistance = distanceToTarget > distanceToBlocker && distanceToTarget <  distanceToBlocker + maxDistance;
                if (inCorrectDistance) {
                    return false;
                }
            }
            return true;
        }
    }
}