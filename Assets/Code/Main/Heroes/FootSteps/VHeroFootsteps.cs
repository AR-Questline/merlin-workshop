using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Terrain;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Threads;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using FMODUnity;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.FootSteps {
    public class VHeroFootsteps : View<Hero> {
        const string IsCrouchingParameterName = "IsCrouching";
        
        const int IsCrouchingParamIndex = 0;
        const int WaterParamIndex = 1;
        const int AdditionalParamsCount = 2;
        [SerializeField] public EventReference footStepEventPath;
        [SerializeField] ComputeShader splatmapsSampleShader;

        FootstepCategoryProvider _footstepProvider;
        FMODParameter[] _fmodParameters;
        int _lastFrame;
        bool _initialized;
        bool _ongoingRequest;
        bool _walkOnWater;

        LayerMask GroundMask => RenderLayers.Mask.CharacterGround;
        
        protected override void OnInitialize() {
            TryInitializeFmodParams();
            _footstepProvider = new(splatmapsSampleShader);
            _initialized = true;
            Target.ListenTo(VCFeetWaterChecker.Events.FeetWaterCollisionChanged, value => _walkOnWater = value, this);
        }

        void OnDestroy() {
            _footstepProvider.Dispose();
        }

        // --- Animator Events
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void TriggerAnimationEvent(UnityEngine.Object obj) {
            if (obj is ARAnimationEvent animationEvent) {
                FMODManager.PlayBodyMovement(animationEvent.ArmorAudio, Target);
            }
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        void FootStep(int isCrouching) {
            if (!_initialized) {
                return;
            }

            // Don't play footsteps multiple times within the same frame or if the is ongoing play request
            if (_ongoingRequest || _lastFrame >= Time.frameCount) {
                return;
            }
            _lastFrame = Time.frameCount;

            PlayFootStep(isCrouching).Forget();
            
            Target.Trigger(Hero.Events.HeroFootstep, isCrouching);
        }

        async UniTaskVoid PlayFootStep(int isCrouching) {
            if (_fmodParameters == null || TryInitializeFmodParams() == false) {
                return;
            }
            bool groundHit = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out var ground, 0.5f, GroundMask, QueryTriggerInteraction.Ignore);
            if (!groundHit) {
                return;
            }

            _ongoingRequest = true;

            float noisiness = Target.HeroStats.FootstepsNoisiness;
            var source = ground.collider.GetComponentInParent<IFootstepSource>();
            if (source != null) {
                source.GetSampleData(ground, out var splatmaps, out var layers, out var uv);
                FootStepsUtils.ClearParameters(_fmodParameters);
                await _footstepProvider.FillFootsteps(splatmaps, layers, uv, noisiness, _fmodParameters, SurfaceType.TerrainGround.FModParameterName);
            } else {
                var surfaceFmodParamName = SurfaceType.TerrainGround.FModParameterName;
                if (ground.collider.TryGetComponentInParent(out MeshSurfaceType meshSurfaceType)) {
                    if (meshSurfaceType.SurfaceType == null) {
                        Log.Important?.Error($"MeshSurfaceType with null SurfaceType! {meshSurfaceType}", meshSurfaceType);
                    } else {
                        surfaceFmodParamName = meshSurfaceType.SurfaceType.FModParameterName;
                    }
                }
                FootStepsUtils.ClearAllAndSet(_fmodParameters, surfaceFmodParamName, noisiness);
            } 
            
            _fmodParameters![IsCrouchingParamIndex] = new FMODParameter(IsCrouchingParameterName, isCrouching);
            _fmodParameters![WaterParamIndex] = new FMODParameter(SurfaceType.TerrainPuddle.FModParameterName, _walkOnWater);
            
            // --- Play footstep sound
            FMODManager.PlayOneShot(footStepEventPath, transform.position, this, _fmodParameters);
            _ongoingRequest = false;
        }
        
        public void PlayClip(UnityEngine.Object o) {
            MainThreadDispatcher.InvokeAsync(() => {
                if (o is FModEventRef template) {
                    FMODManager.PlayAttachedOneShotWithParameters(template, gameObject, Target.ParentTransform);
                }
            });
        }
        
        bool TryInitializeFmodParams() {
            return FootStepsUtils.TryInitialize(ref _fmodParameters, footStepEventPath, AdditionalParamsCount, SurfaceType.TerrainPuddle.FModParameterName);
        }
    }
}
