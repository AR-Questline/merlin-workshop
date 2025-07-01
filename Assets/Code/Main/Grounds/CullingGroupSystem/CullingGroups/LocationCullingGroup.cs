using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Unity.Mathematics;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    public class LocationCullingGroup : BaseCullingGroup {
        public const int LastBand = 5;
        const float DistanceBandsMinScale = 0.5f;
        const float DistanceBandsMaxScale = 3f;

        IEventListener _distanceCullingGroupListener;
        readonly DistanceCullingSetting _distanceCullingSetting;
        readonly CullingDistanceMultiplierService _cullingDistanceMultipliersService;
        
        public static readonly float[] LocationDistanceBands = {
            // 0
            4, 
            // 1
            15, 
            // 2
            50, 
            // 3
            75,
            // 4
            90, 
            // 5
        };
        
        public static bool InRestockBand(int band) => band > 2;
        public static bool InActiveLogicBands(int band) => band < 5;
        public static bool InNpcVisibilityBand(int band) => band < 5;
        public static bool InBandToDiscardRagdoll(int band) => band >= 4;
        public static bool InHairTransparentSurfaceBand(int band) => band < 2;
        public static bool InAnimationRiggingBand(int band) => band < 2;
        public static bool InEyesEnabledBand(int band) => band < 1;
        public static bool InOverlapRecoveryBand(int band) => band <= 1;

        public LocationCullingGroup() : this(LocationDistanceBands) { }

        protected LocationCullingGroup(float[] distanceBands, float hysteresisPercent = 0.15f) : base(distanceBands, hysteresisPercent) {
            _distanceCullingSetting = World.Any<DistanceCullingSetting>();
            _cullingDistanceMultipliersService = World.Services.Get<CullingDistanceMultiplierService>();
            if (_distanceCullingSetting == null) {
                Log.Debug?.Error($"No {nameof(DistanceCullingSetting)} when instantiating {nameof(LocationCullingGroup)}. Using highest quality setting");
                RefreshDistanceBands(1, 1, true);
                return;
            }
            if (_cullingDistanceMultipliersService == null) {
                Log.Debug?.Error($"No {nameof(CullingDistanceMultiplierService)} when instantiating {nameof(LocationCullingGroup)}. Using highest quality setting");
                RefreshDistanceBands(1, 1, true);
                return;
            }
            _distanceCullingGroupListener = _distanceCullingSetting.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged);
            _cullingDistanceMultipliersService.OnCullingDistanceMultiplierChanged += OnDistanceCullingMultiplierChanged;
            OnSettingChanged(_distanceCullingSetting);
        }

        void OnSettingChanged(Setting setting) {
            var distanceCullingSetting = (DistanceCullingSetting) setting;
            RefreshDistanceBands(distanceCullingSetting.Value, _cullingDistanceMultipliersService.Multiplier, _cullingDistanceMultipliersService.ClampMultiplier);
        }
        
        void OnDistanceCullingMultiplierChanged(float distanceCullingMultiplier, bool clampAreaMultiplier) {
            var multiplierFromSettings = _distanceCullingSetting?.Value ?? 1;
            RefreshDistanceBands(multiplierFromSettings, distanceCullingMultiplier, clampAreaMultiplier);
        }

        void RefreshDistanceBands(float distanceCullingValue01, float areaMultiplier, bool clampAreaMultiplier) {
            RefreshDistanceBands(GetLocationDistanceBandsScaled(distanceCullingValue01, areaMultiplier, clampAreaMultiplier), HysteresisPercent);
        }
        
        float[] GetLocationDistanceBandsScaled(float distanceCullingSettingsValue01, float areaMultiplier, bool clampAreaMultiplier) {
            var multiplier = distanceCullingSettingsValue01.Remap01(DistanceBandsMinScale, 1);
            multiplier *= areaMultiplier;
            if (clampAreaMultiplier) {
                multiplier = math.clamp(multiplier, DistanceBandsMinScale, DistanceBandsMaxScale);
            }
            return GetOriginalBands(multiplier);
        }
        
        public override void Dispose() {
            base.Dispose();
            if (_distanceCullingGroupListener != null) {
                World.EventSystem.RemoveListener(_distanceCullingGroupListener);
            }
            _distanceCullingGroupListener = null;
            if (_cullingDistanceMultipliersService != null) {
                _cullingDistanceMultipliersService.OnCullingDistanceMultiplierChanged -= OnDistanceCullingMultiplierChanged;
            }
        }
    }
}