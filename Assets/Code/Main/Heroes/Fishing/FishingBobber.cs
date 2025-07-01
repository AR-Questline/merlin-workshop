using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.Water;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Awaken.Utility;
using Awaken.Utility.PhysicUtils;
using FMODUnity;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Heroes.Fishing {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class FishingBobber : MonoBehaviour {
        const string FishFightingIntensityParameter = "WaterSplashForce";
        const float MinFishFightingIntensityValue = 50f;
        const float MaxFishFightingIntensityValue = 100f;
        
        static readonly IFishVolume[] ReusableFishVolumes = new IFishVolume[10];

        [SerializeField] float bobbingStrength = 0.01f;
        [SerializeField] float bobbingFrequency = 2f;
        [SerializeField] float catchStrength = 0.2f;
        [SerializeField] float catchBobFactor = 0.2f;
        [SerializeField] AnimationCurve catchPercent = new(new Keyframe(0, 0), new Keyframe(0.3f, 1), new Keyframe(1.3f, 1), new Keyframe(1.8f, 0));
        [SerializeField] AnimationCurve fakeCatchPercent = new(new Keyframe(0, 0), new Keyframe(0.3f, 0.4f), new Keyframe(0.7f, 0));

        [FoldoutGroup("Water Sampling"), SerializeField] WaterSurfaceSampler.Settings waterSampleSettings;
        
        public IFishVolume CurrentFishVolume { get; private set; }

        float _catchTime;
        float _fakeCatchTime;
        
        Rigidbody _rigidbody;
        SphereCollider _collider;
        [UnityEngine.Scripting.Preserve] WaterSurface _hitWaterSurface;

        bool _thrown;
        
        bool _inWater;
        float _inWaterStartTime;
        Vector3 _inWaterPosition;
        
        bool _catching;
        bool _fakeCatching;
        float _catchingStartTime;

        float _nextCatchIn;

        CommonReferences _commonReferences;
        FishingFSM _fishingFSM;
        ARFmodEventEmitter _emitter;
        WaterSurfaceSampler _waterSurfaceSampler;
        IEventListener _miniGameStartListener;
        IEventListener _miniGameTickListener;
        
        ref readonly FishingAudio Audio => ref _commonReferences.AudioConfig.FishingAudio;

        public Vector3 InWaterPosition => _inWaterPosition;

        void Awake() {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            
            _catchTime = catchPercent[catchPercent.length - 1].time;
            _fakeCatchTime = fakeCatchPercent[fakeCatchPercent.length - 1].time;
            
            _commonReferences = World.Services.Get<CommonReferences>();
            _fishingFSM = Hero.Current.Element<FishingFSM>();
            _emitter = GetComponent<ARFmodEventEmitter>();
            //_emitter.EventStopTrigger = EmitterGameEvent.ObjectDestroy;
            _waterSurfaceSampler = new WaterSurfaceSampler(waterSampleSettings);
            
            _miniGameStartListener = World.EventSystem.ListenTo(EventSelector.AnySource, FishingMiniGame.Events.OnMiniGameStart, StartFishFightingAudio);
            _miniGameTickListener = World.EventSystem.ListenTo(EventSelector.AnySource, FishingMiniGame.Events.OnMiniGameTick, UpdateFishFightingAudio);
        }

        void Update() {
            if (!_thrown) {
                return;
            }
            
            if (_rigidbody.IsSleeping()) {
                StopPhysics();
            }

            if (_inWater) {
                if (!_catching) {
                    UpdateWaitingForCatch();
                }
                UpdateBobbingAndCatching();
            }
        }

        void OnTriggerEnter(Collider other) {
            CheckForWaterHit(other.gameObject);
        }

        void OnCollisionEnter(Collision col) {
            CheckForWaterHit(col.gameObject);
        }

        void StartFishFightingAudio() {
            //var eventReference = CommonReferences.Get.AudioConfig.FishingAudio.fishFighting;
            //_emitter.PlayNewEventWithPauseTracking(eventReference, new FMODParameter(FishFightingIntensityParameter, MaxFishFightingIntensityValue));
        }

        void UpdateFishFightingAudio(FishingMiniGame fishingMiniGame) {
            //float intensity = math.lerp(MinFishFightingIntensityValue, MaxFishFightingIntensityValue, fishingMiniGame.FishHealth / fishingMiniGame.MaxFishHealth);
            //_emitter.SetParameter(FishFightingIntensityParameter, intensity);
        }

        void CheckForWaterHit(GameObject hitObject) {
            if (!_inWater && hitObject.layer == RenderLayers.Water) {
                _hitWaterSurface = hitObject.GetComponentInChildren<WaterSurface>();
                RegisterWaterHit();
            }
        }

        void RegisterWaterHit() {
            OnWaterHit(_rigidbody.position);
            StopPhysics();
            PlanNextCatch();
        }

        void UpdateWaitingForCatch() {
            if (_nextCatchIn <= 0) {
                return;
            }
            
            _nextCatchIn -= Time.deltaTime;
            if (_nextCatchIn > 0) {
                return;
            }
            
            _nextCatchIn = -1;
            ref readonly FishingData data = ref CommonReferences.Get.FishingData;
            float density = GetCumulativeFishDensity();
            float trueCatchChance = data.fishingChanceLimit.Clamp(density);
            bool fake = !RandomUtil.WithProbability(trueCatchChance);
            StartCatching(fake);
        }
        
        void UpdateBobbingAndCatching() {
            float offset = Mathf.Sin((Time.time - _inWaterStartTime) * bobbingFrequency) * bobbingStrength;
            if (_catching) {
                var time = Time.time - _catchingStartTime;
                if (time <= (_fakeCatching ? _fakeCatchTime : _catchTime)) {
                    var curve = _fakeCatching ? fakeCatchPercent : this.catchPercent;
                    var catchPercent = curve.Evaluate(time);
                    offset *= Mathf.Lerp(1, catchBobFactor, catchPercent); // reduce bobbing when catching
                    offset += catchPercent * catchStrength;
                } else {
                    if (_fishingFSM.CurrentStateType is HeroStateType.FishingBite or HeroStateType.FishingBiteLoop) {
                        Hero.Current.Trigger(FishingFSM.Events.Fail, Hero.Current);
                    }
                    
                    _catching = false;
                    PlanNextCatch(false);
                }
            }

            _waterSurfaceSampler.RequestSample(_hitWaterSurface, _inWaterPosition);
            _waterSurfaceSampler.ProgressEasing(Time.deltaTime);
            transform.position = _inWaterPosition + Vector3.up * (_waterSurfaceSampler.EasedOffset.y - offset);
        }
        
        public bool TryGetFish(out FishData.FightingFish fish) {
            if (!_inWater) {
                fish = default;
                return false;
            }
            
            CollectFishVolumes(_inWaterPosition, 0.5f, ReusableFishVolumes, out int count);
            int index = RandomUtil.WeightedSelect(0, count - 1, i => ReusableFishVolumes[i].GetDensity(_inWaterPosition));
            CurrentFishVolume = ReusableFishVolumes[index];
            fish = CurrentFishVolume.FishData().ToFightingFish();
            Array.Clear(ReusableFishVolumes, 0, count);
            return true;
        }
        
        public void Throw(Vector3 to, float velocity, bool highShot) {
            _thrown = true;
            ClearWaterData();
            ResumePhysics();
            _rigidbody.linearVelocity = ArcherUtils.ShotVelocity(new ShotData(transform.position, to, velocity, highShot));
        }
        
        public void Catch(Vector3 to, float velocity) {
            _thrown = false;
            ClearWaterData();
            StopPhysics();
        }
        
        void StopPhysics() {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _collider.enabled = false;
        }
        
        void ResumePhysics() {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _collider.enabled = true;
        }

        void ClearWaterData() {
            _inWater = false;
            _inWaterPosition = Vector3.zero;
            _inWaterStartTime = 0;
        }

        void OnWaterHit(Vector3 position) {
            _inWater = true;
            _inWaterPosition = position;
            _inWaterStartTime = Time.time;
            FMODManager.PlayOneShot(Audio.bobberHitWater, _inWaterPosition);
            Hero.Current.Trigger(FishingFSM.Events.BobberHitWater, Hero.Current);
        }
        
        void PlanNextCatch(bool invokeGetVolumeMethod = true) {
            var density = GetCumulativeFishDensity(invokeGetVolumeMethod);
            ref readonly FishingData data = ref CommonReferences.Get.FishingData;
            _nextCatchIn = data.fishingTimeLimit.Clamp(data.fishingTimeBase.RandomPick() / density);
        }
        
        void StartCatching(bool fake) {
            if (_catching || _fishingFSM.CurrentStateType != HeroStateType.FishingIdle) {
                return;
            }

            if (!fake) {
                _fishingFSM.SetCurrentState(HeroStateType.FishingBite);
            }

            RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.Short);
            if (!fake) {
                RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.VeryLong);
            }

            _catching = true;
            _fakeCatching = fake;
            _catchingStartTime = Time.time;
            FMODManager.PlayOneShot(fake ? Audio.bobberSubmergeFakeCatch : Audio.bobberSubmergeWithCatch, _inWaterPosition);
        }

        float GetCumulativeFishDensity(bool invokeGetVolumeMethod = false) {
            CollectFishVolumes(_inWaterPosition, 0.5f, ReusableFishVolumes, out int count);

            if (count == 0) {
                return CommonReferences.Get.FishingData.genericFishDensity;
            }
            
            float density = 0;
            for (int i = 0; i < count; i++) {
                density += ReusableFishVolumes[i].GetDensity(_inWaterPosition);
                if (invokeGetVolumeMethod) {
                    ReusableFishVolumes[i].OnGetVolume();
                }
                ReusableFishVolumes[i] = null;
            }
            return density;
        }

        static void CollectFishVolumes(Vector3 position, float radius, IFishVolume[] volumes, out int count) {
            count = 0;
            foreach (var colliderHit in PhysicsQueries.OverlapSphere(position, radius, FishVolume.Mask)) {
                if (colliderHit.TryGetComponent(out FishVolume volume)) {
                    volumes[count++] = volume;
                    if (count == volumes.Length - 1) {
                        Log.Important?.Error("Too many overlapping fish volumes!");
                        break;
                    }
                }
            }

            if (count == 0) {
                volumes[count++] = GenericFishVolume.Instance;
            }
            
            Hero.Current.Trigger(CharacterFishingRod.Events.OnFishVolumesCulminated, ReusableFishVolumes);
        }

        protected void OnDestroy() {
            World.EventSystem.DisposeListener(ref _miniGameStartListener);
            World.EventSystem.DisposeListener(ref _miniGameTickListener);
            
            _waterSurfaceSampler.Dispose();
        }
    }
}