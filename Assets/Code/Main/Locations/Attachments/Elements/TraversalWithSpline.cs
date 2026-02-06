using Awaken.TG.Assets;
using Awaken.TG.Graphics.ScriptedEvents.Triggers;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Splines;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class TraversalWithSpline : Element<Location>, IRefreshedByAttachment<TraversalWithSplineAttachment> {
        public override ushort TypeForSerialization => SavedModels.TraversalWithSpline;
        
        const float PredictionAheadFactor = 20;
        const float InitialFovValue = 125f;
        const float DesiredFovValue = 70f;
        const float InitialDampingValue = 20f;
        const float DesiredDampingValue = 0.25f;
        const float InitialPositionOffset = 6;

        TraversalWithSplineAttachment _spec;
        HeroTrigger _heroTrigger;
        SplineContainer _splineContainer;
        SplineExtrude _splineExtrude;
        MeshRenderer _splineMeshRenderer;
        ARFmodEventEmitter _eventEmitter;
        float _splineLength;
        
        bool _isTraveling;
        bool _isDisappearing;
        float _moveSpeed;
        float _previousTValue;
        float _timeElapsed;
        
        GameObject _fastTravelVisual;
        CinemachineVirtualCamera _virtualCamera;
        CinemachineTransposer _transposer;
        VCManualDissolveController _dissolveController;
        UIState _uiState;
        
        ARAsyncOperationHandle<GameObject> _visualHandle;
        AsyncOperationHandle<GameObject> _discardVfxPreloadHandle;

        CharacterManualInvisibility _invisibility;
        HeroDirectionalBlur _directionalBlur;
        
        public void InitFromAttachment(TraversalWithSplineAttachment spec, bool isRestored) {
            _spec = spec;
            _splineContainer = spec.Spline;
            _splineLength = _splineContainer.Spline.GetLength();
        }
        
        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(t => {
                _splineExtrude = t.GetComponentInChildren<SplineExtrude>(true);
                if (_splineExtrude != null) {
                    _splineMeshRenderer = _splineExtrude.GetComponent<MeshRenderer>();
                }
                
                _eventEmitter = t.GetComponentInChildren<ARFmodEventEmitter>(true);
                
                _heroTrigger = t.GetComponentInChildren<HeroTrigger>(true);
                if (_heroTrigger == null) {
                    Log.Critical?.Error("FastTravelWithTraversal without hero trigger!", t);
                    Discard();
                    return;
                }
                _heroTrigger.OnHeroEnter += OnStart;
            });
        }

        void OnStart() {
            if (_isTraveling || !ParentModel.Interactable) {
                return;
            }

            bool canMovementBeOverriden = Hero.Current.MovementSystem?.CanCurrentlyBeOverriden ?? true;
            if (!canMovementBeOverriden) {
                return;
            }

            _isTraveling = true;
            _isDisappearing = false;
            _previousTValue = InitialPositionOffset / _splineLength;
            _moveSpeed = 0;
            _timeElapsed = 0;

            if (_eventEmitter != null) {
                // _eventEmitter.PlayNewEventWithPauseTracking(_spec.traversalSFX);
            }
            
            _uiState = UIState.BlockInput.WithHUDState(HUDState.EverythingHidden);
            World.Only<UIStateStack>().PushState(_uiState, this);
            
            UniTask<IPooledInstance>? vfxTask = null;
            if (_spec.spawnVfx.IsSet) {
                EvaluateSpline(_splineContainer, _previousTValue / 2, out var vfxPosition, out var vfxRotation);
                vfxTask = PrefabPool.Instantiate(_spec.spawnVfx, vfxPosition, vfxRotation, automaticallyActivate: false);
            }

            if (_splineExtrude != null) {
                _splineMeshRenderer.enabled = true;
                _splineExtrude.enabled = true;
            }
            
            EvaluateSpline(_splineContainer, _previousTValue, out var position, out var rotation);
            _visualHandle = _spec.fastTravelVisual.LoadAsset<GameObject>();
            _visualHandle.OnComplete(h => OnVisualLoaded(h, vfxTask, position, rotation).Forget());
        }

        async UniTaskVoid OnVisualLoaded(ARAsyncOperationHandle<GameObject> h, UniTask<IPooledInstance>? vfxTask, Vector3 position, Quaternion rotation) {
            if (h.Result == null || h.Status == AsyncOperationStatus.Failed) {
                Log.Critical?.Error("Failed to load fast travel visual");
                _spec.fastTravelVisual.ReleaseAsset();
                if (vfxTask.HasValue) {
                    (await vfxTask.Value).Release();
                }
                return;
            }

            if (vfxTask.HasValue) {
                var spawnVfx = await vfxTask.Value;
                spawnVfx.Instance.gameObject.SetActive(true);
                spawnVfx.Return(10f).Forget();
            }
            
            if (_spec.discardVfx.IsSet) {
                _discardVfxPreloadHandle = _spec.discardVfx.PreloadLight<GameObject>();
            }

            _fastTravelVisual = Object.Instantiate(h.Result, position, rotation);
            _virtualCamera = _fastTravelVisual.GetComponentInChildren<CinemachineVirtualCamera>();
            _transposer = _virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            _dissolveController = _fastTravelVisual.GetComponentInChildren<VCManualDissolveController>();
            if (_dissolveController != null) {
                _dissolveController.Attach(World.Services, ParentModel, ParentModel.MainView);
                _dissolveController.SwitchVisibility(true);
            }
            _directionalBlur = AddElement(new HeroDirectionalBlur(World.Services.Get<SpecialPostProcessService>().VolumeFishMetro, _fastTravelVisual.transform, 2f));
            
            Hero.Current.Hide();
            _invisibility = Hero.Current.AddElement<CharacterManualInvisibility>();
            Hero.Current.TrySetMovementType(out HeroSplineTraversalMovement fastTravel);
            fastTravel.SetFollowObject(_fastTravelVisual);
            
            ParentModel.GetOrCreateTimeDependent().WithAlwaysUpdate(MovementUpdate);
        }

        void MovementUpdate(float deltaTime) {
            _timeElapsed += deltaTime;
            if (_moveSpeed < _spec.maxMoveSpeed) {
                _moveSpeed = math.clamp(_moveSpeed + deltaTime * _spec.acceleration, 0, _spec.maxMoveSpeed);
            }
            // Move along spline at provided interval and distance
            float tValue = _previousTValue + (deltaTime * _moveSpeed) / _splineLength;
            if (tValue >= 1) {
                OnEnd();
                return;
            }

            if (_timeElapsed <= 1) {
                float lerpFactor = (_timeElapsed - 0.25f) / 0.5f;
                _virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(InitialFovValue, DesiredFovValue, lerpFactor);
                _transposer.m_ZDamping = Mathf.Lerp(InitialDampingValue, DesiredDampingValue, lerpFactor);
            }

            Vector3 previousPosition = _fastTravelVisual.transform.position;
            EvaluateSpline(_splineContainer, tValue, out var position, out var rotation);
            _fastTravelVisual.transform.SetPositionAndRotation(position, rotation);
            _previousTValue = tValue;

            _directionalBlur.SetBlurVelocity(position - previousPosition);
            
            if (_isDisappearing) {
                return;
            }

            float timeRemaining = (_splineLength * (1 - tValue)) / _moveSpeed;
            if (timeRemaining <= 1f && !_isDisappearing) {
                _isDisappearing = true;
                if (_dissolveController != null) {
                    _dissolveController.SwitchVisibility(false);
                }

                if (_spec.onPathEndVFX.IsSet) {
                    EvaluateSpline(_splineContainer, 1, out position, out rotation);
                    PrefabPool.InstantiateAndReturn(_spec.onPathEndVFX, position, rotation).Forget();
                }
            }
        }

        void OnEnd() {
            if (_eventEmitter != null) {
                // _eventEmitter.Stop();
            }
            
            _directionalBlur?.Discard();
            _directionalBlur = null;
            
            World.Only<UIStateStack>().RemoveState(_uiState);
            _uiState = null;
            ParentModel.GetTimeDependent()?.WithoutAlwaysUpdate(MovementUpdate);
            
            EvaluateSpline(_splineContainer, 1, out var position, out var rotation);
            rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            Hero.Current.ReturnToDefaultMovement();
            Hero.Current.TeleportTo(position, rotation, null, true);
            Hero.Current.Show();
            _invisibility?.Discard();
            _invisibility = null;

            if (_spec.discardVfx.IsSet) {
                var vfxPosition = Ground.SnapToGround(position);
                PrefabPool.InstantiateAndReturn(_spec.discardVfx, vfxPosition, rotation).Forget();
                _discardVfxPreloadHandle.Release();
                _discardVfxPreloadHandle = default;
            }
            
            if (_splineExtrude != null) {
                _splineMeshRenderer.enabled = false;
                _splineExtrude.enabled = false;
            }

            _virtualCamera = null;
            _transposer = null;
            _dissolveController = null;
            
            Object.Destroy(_fastTravelVisual);
            _fastTravelVisual = null;
            
            _visualHandle.Release();
            _visualHandle = default;
            
            _isTraveling = false;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                _heroTrigger.OnHeroEnter -= OnStart;
            }
        }

        void EvaluateSpline(SplineContainer spline, float t, out Vector3 desiredPosition, out Quaternion desiredRotation) {
            desiredPosition = spline.EvaluatePosition(t);

            float prediction = math.clamp(t + (t - _previousTValue) * PredictionAheadFactor, 0, 1);
            var tangent = spline.EvaluateTangent(prediction);
            var up = spline.EvaluateUpVector(prediction);
            desiredRotation = Quaternion.LookRotation(tangent, up);
        }
    }
}