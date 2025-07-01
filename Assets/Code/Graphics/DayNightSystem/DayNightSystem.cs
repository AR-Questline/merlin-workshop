using System;
using System.Collections;
using System.IO;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using Awaken.Utility.Times;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.VisualScripting;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Awaken.TG.Graphics.DayNightSystem {
    [ExecuteAlways]
    public class DayNightSystem : StartDependentView<GameRealTime> {
        const int TotalMinutesInDay = 1440;
        const string SkyDayMapProp = "_SkyDayMap";
        const string SkyNightMap = "_SkyNightMap";
        const string SkyWyrdnessMap = "_SkyWyrdnessMap";
        const string SkyWyrdnessMaskMap = "_SkyWyrdnessMaskMap";

        #region Time

        [TabGroup("TAB1", "TIME", SdfIconType.ClockFill)]
        [DisplayAsString(TextAlignment.Center, EnableRichText = true, FontSize = 18)]
        [ShowInInspector, HideLabel, PropertyOrder(-1)]
        string CurrentTime => $"{_hour:D2}:{_minute:D2}";

        [TabGroup("TAB1", "TIME", SdfIconType.ClockFill)]
        [ProgressBar(0, 1, r: 1, g: 1, b: 1, Height = 20, ColorGetter = nameof(ODIN_GetProgressBarColor))]
        [ShowInInspector, HideLabel]
        float _editorTimeOfDay = 0.5f;

        float TimeOfDay => GenericTarget != null ? Target.WeatherTime.DayTime : _editorTimeOfDay;
        int _hour;
        int _minute;

        #endregion

        #region Directional Lights

        // --- Light Angle ---
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/ANGLE", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, Range(0, 360), LabelWidth(160)]
        float horizontalAngle;

        float _editorHorizontalAngle = 0f;
        public float HorizontalAngle => _editorHorizontalAngle == 0 ? horizontalAngle : _editorHorizontalAngle;

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/ANGLE", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, Range(0, 45), LabelWidth(160)]
        float inclination;

        // --- Light Intensity ---
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY/G1"), PropertyOrder(0)]
        [ShowInInspector, LabelWidth(160)]
        int LightIntensity => (int)lightIntensityCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY/G1"), PropertyOrder(1)]
        [SerializeField, HideLabel]
        AnimationCurve lightIntensityCurve = AnimationCurve.Linear(0, 100000, 1, 100000);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY/G2"), PropertyOrder(2)]
        [ShowInInspector, LabelWidth(160)]
        int LightIntensityMultiplier => (int)lightIntensityMultiplierCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY/G2"), PropertyOrder(3)]
        [SerializeField, HideLabel]
        AnimationCurve lightIntensityMultiplierCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY/G3"), PropertyOrder(4)]
        [ShowInInspector, LabelWidth(160)]
        int LightVolumetricMultiplier => (int)lightVolumetricMultiplierCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/INTENSITY/G3"), PropertyOrder(5)]
        [SerializeField, HideLabel]
        AnimationCurve lightVolumetricMultiplierCurve = AnimationCurve.Linear(0, 16, 1, 16);

        // --- Celestial Body ---
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY/G1"), PropertyOrder(6)]
        [ShowInInspector, LabelWidth(160)]
        float SunFlareSize => sunFlareSizeCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY/G1"), PropertyOrder(7)]
        [SerializeField, HideLabel]
        AnimationCurve sunFlareSizeCurve = AnimationCurve.Linear(0, 16, 1, 16);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY/G2"), PropertyOrder(8)]
        [ShowInInspector, LabelWidth(160)]
        float SunFlareMultiplier => sunFlareMultiplierCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/CELESTIAL BODY/G2"), PropertyOrder(9)]
        [SerializeField, HideLabel]
        AnimationCurve sunFlareMultiplierCurve = AnimationCurve.Linear(0, 0.005f, 1, 0.005f);

        // --- Light Color ---
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/COLOR", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/COLOR/G1"), PropertyOrder(10)]
        [ShowInInspector, LabelWidth(160)]
        int LightTemperature => (int)(_sunLight == null ? 0 : _sunLight.colorTemperature);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/COLOR", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/COLOR/G1"), PropertyOrder(11)]
        [SerializeField, HideLabel]
        AnimationCurve lightTemperatureCurve = AnimationCurve.Linear(0, 5500, 1, 5500);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/COLOR", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/COLOR/G2"), PropertyOrder(12)]
        [ShowInInspector, LabelWidth(160)]
        Color LightTint => lightTintGradient.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/COLOR", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/COLOR/G2"), PropertyOrder(13)]
        [SerializeField, HideLabel, LabelWidth(160), ShowIf("enableLightTint")]
        Gradient lightTintGradient = new Gradient {
            alphaKeys = new[] {
                new GradientAlphaKey(1, 0f),
                new GradientAlphaKey(1, 1f)
            },
            colorKeys = new[] {
                new GradientColorKey(new Color(0.2509804f, 0.5019608f, 1f), 0f),
                new GradientColorKey(new Color(0.2509804f, 0.5019608f, 1f), 0.2f),
                new GradientColorKey(new Color(1f, 0.7450981f, 0.5019608f), 0.25f),
                new GradientColorKey(new Color(1f, 1f, 1f), 0.3f),
                new GradientColorKey(new Color(1f, 1f, 1f), 0.7f),
                new GradientColorKey(new Color(1f, 0.7450981f, 0.5019608f), 0.75f),
                new GradientColorKey(new Color(0.2509804f, 0.5019608f, 1f), 0.8f),
                new GradientColorKey(new Color(0.2509804f, 0.5019608f, 1f), 1f)
            }
        };

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/COLOR", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/COLOR/G3"), PropertyOrder(14)]
        [ShowInInspector, LabelWidth(160)]
        Color MoonSurfaceTint => moonSurfaceTintGradient.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/COLOR", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/COLOR/G3"), PropertyOrder(15)]
        [SerializeField, HideLabel, LabelWidth(160)]
        Gradient moonSurfaceTintGradient = new() {
            alphaKeys = new[] {
                new GradientAlphaKey(1, 0f),
                new GradientAlphaKey(1, 1f)
            },
            colorKeys = new[] {
                new GradientColorKey(new Color(0.7058824f, 0.7860422f, 1f), 0f),
                new GradientColorKey(new Color(1, 1, 1), 0.5f),
                new GradientColorKey(new Color(0.7058824f, 0.7860422f, 1f), 0f)
            }
        };

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/COLOR", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, LabelWidth(160), PropertyOrder(16)]
        [Tooltip("Enable directional light tint color to additional control")]
        bool enableLightTint;

        // --- Lens Flare ---
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/LENS FLARE", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/LENS FLARE/G1"), PropertyOrder(17)]
        [SerializeField, LabelWidth(160)]
        bool enableLensFlare;

        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/LENS FLARE", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [HorizontalGroup("TAB2/DIRECTIONAL LIGHTS/LENS FLARE/G1"), PropertyOrder(18)]
        [SerializeField, HideLabel]
        AnimationCurve lightLensFlare = AnimationCurve.Linear(0, 1, 1, 1);
        
        // --- Shadows ---
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/SHADOW LIGHT", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, Range(0, 45), LabelWidth(160)]
        float shadowMinimumAngle = 30;
        
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/SHADOW LIGHT", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, Range(0, 1), LabelWidth(160)]
        float shadowAngleSnapFactor = 1;
        
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/SHADOW LIGHT", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, Range(1, 300), LabelWidth(160)]
        int shadowIntensityBlendMinutes = 60;
        
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/SHADOW LIGHT", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, LabelWidth(160)]
        bool disableShadowCookiesAtNight = true;
        
        [FoldoutGroup("TAB2/DIRECTIONAL LIGHTS/SHADOW LIGHT", expanded: true)]
        [TabGroup("TAB2", "DIRECTIONAL LIGHTS", SdfIconType.LightbulbFill)]
        [SerializeField, LabelWidth(160)]
        bool shadowCasterFollowPoV = false;
        
        #endregion

        #region PostProcesses

        // --- Exposure ---
        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G0"), PropertyOrder(0)]
        [SerializeField, LabelWidth(160)]
        ExposureType exposureType;

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G1"), PropertyOrder(1)]
        [ShowInInspector, LabelWidth(160), ShowIf("ODIN_IsFixedExposure")]
        float Exposure => exposureCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G1"), PropertyOrder(2)]
        [SerializeField, HideLabel, ShowIf("ODIN_IsFixedExposure")]
        AnimationCurve exposureCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G1"), PropertyOrder(1)]
        [ShowInInspector, LabelWidth(160), ShowIf("ODIN_IsAutomaticExposure")]
        float ExposureLimitMin => exposureLimitMinCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G1"), PropertyOrder(2)]
        [SerializeField, HideLabel, ShowIf("ODIN_IsAutomaticExposure")]
        AnimationCurve exposureLimitMinCurve = AnimationCurve.Linear(-2, 15, -2, -2);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G2"), PropertyOrder(3)]
        [ShowInInspector, LabelWidth(160), ShowIf("ODIN_IsAutomaticExposure")]
        float ExposureLimitMax => exposureLimitMaxCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G2"), PropertyOrder(4)]
        [SerializeField, HideLabel, ShowIf("ODIN_IsAutomaticExposure")]
        AnimationCurve exposureLimitMaxCurve = AnimationCurve.Linear(-2, 15, -2, -2);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G3"), PropertyOrder(5)]
        [ShowInInspector, LabelWidth(160), ShowIf("ODIN_IsAutomaticExposure")]
        float ExposureHistogramMin => exposureHistogramMinCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G3"), PropertyOrder(6)]
        [SerializeField, HideLabel, ShowIf("ODIN_IsAutomaticExposure")]
        AnimationCurve exposureHistogramMinCurve = AnimationCurve.Linear(0, 50, 1, 50);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G4"), PropertyOrder(7)]
        [ShowInInspector, LabelWidth(160), ShowIf("ODIN_IsAutomaticExposure")]
        float ExposureHistogramMax => exposureHistogramMaxCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G4"), PropertyOrder(8)]
        [SerializeField, HideLabel, ShowIf("ODIN_IsAutomaticExposure")]
        AnimationCurve exposureHistogramMaxCurve = AnimationCurve.Linear(0, 100, 1, 100);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G5"), PropertyOrder(9)]
        [ShowInInspector, LabelWidth(160), ShowIf("ODIN_IsFixedOrPhysicalCamera")]
        float ExposureCompensation => exposureCompensationCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/EXPOSURE", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/EXPOSURE/G5"), PropertyOrder(10)]
        [SerializeField, HideLabel, ShowIf("ODIN_IsFixedOrPhysicalCamera")]
        AnimationCurve exposureCompensationCurve = AnimationCurve.Linear(0, 0, 1, 0);
        
        // --- Color Curves ---
        // [FoldoutGroup("TAB2/POST PROCESSES/COLOR CURVE", expanded: true)]
        // [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        // [HorizontalGroup("TAB2/POST PROCESSES/COLOR CURVE/G1"), PropertyOrder(16)]
        // [SerializeField]
        // public AnimationCurve colorCurveDay = AnimationCurve.Linear(0, 0, 1, 1);
        //
        // [FoldoutGroup("TAB2/POST PROCESSES/COLOR CURVE", expanded: true)]
        // [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        // [HorizontalGroup("TAB2/POST PROCESSES/COLOR CURVE/G2"), PropertyOrder(17)]
        // [SerializeField]
        // public AnimationCurve colorCurveNight = AnimationCurve.Linear(0, 0, 1, 1);
        //
        // [FoldoutGroup("TAB2/POST PROCESSES/COLOR CURVE", expanded: true)]
        // [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        // [HorizontalGroup("TAB2/POST PROCESSES/COLOR CURVE/G3"), PropertyOrder(18)]
        // [HideIf("areCurvesValid", false)]
        // [InfoBox("Number of keys in ColorCurveDay and ColorCurveNight does not match!", InfoMessageType.Warning)]
        // public bool areCurvesValid;

        // --- Post Adjustments ---
        [FoldoutGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS/G1"), PropertyOrder(19)]
        [ShowInInspector, LabelWidth(160)]
        float PostExposure => postExposureCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS/G1"), PropertyOrder(20)]
        [SerializeField, HideLabel]
        AnimationCurve postExposureCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [FoldoutGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS/G2"), PropertyOrder(21)]
        [ShowInInspector, LabelWidth(160)]
        Color ColorFilter => colorFilterGradient.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS/G2"), PropertyOrder(22)]
        [SerializeField, HideLabel, LabelWidth(160)]
        Gradient colorFilterGradient = new() {
            alphaKeys = new[] {
                new GradientAlphaKey(1, 0f),
                new GradientAlphaKey(1, 1f)
            },
            colorKeys = new[] {
                new GradientColorKey(new Color(0.7058824f, 0.7860422f, 1f), 0f),
                new GradientColorKey(new Color(1, 1, 1), 0.5f),
                new GradientColorKey(new Color(0.7058824f, 0.7860422f, 1f), 0f)
            }
        };

        [FoldoutGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS/G3"), PropertyOrder(23)]
        [ShowInInspector, LabelWidth(160)]
        float IndirectDiffuse => indirectDiffuseCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/POST ADJUSTMENTS/G3"), PropertyOrder(24)]
        [SerializeField, HideLabel]
        AnimationCurve indirectDiffuseCurve = AnimationCurve.Linear(0, 1, 1, 1);

        // --- Fog ---
        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G1"), PropertyOrder(25)]
        [ShowInInspector, LabelWidth(160)]
        int FogDensity => (int)fogDensityCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G1"), PropertyOrder(26)]
        [ShowInInspector]
        [SerializeField, HideLabel]
        AnimationCurve fogDensityCurve = AnimationCurve.Linear(0, 256, 1, 256);

        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G2"), PropertyOrder(27)]
        [ShowInInspector, LabelWidth(160)]
        int FogHeight => (int)fogHeightCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G2"), PropertyOrder(28)]
        [SerializeField, HideLabel]
        AnimationCurve fogHeightCurve = AnimationCurve.Linear(0, 512, 1, 512);
        
        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G3"), PropertyOrder(29)]
        [ShowInInspector, LabelWidth(160)]
        int FogAnisotropy => (int)fogAnisotropyCurve.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G3"), PropertyOrder(30)]
        [SerializeField, HideLabel]
        AnimationCurve fogAnisotropyCurve = AnimationCurve.Linear(0, 0, 1, 0);

        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G4"), PropertyOrder(31)]
        [ShowInInspector, LabelWidth(160)]
        Color fogAlbedo => fogAlbedoGradient.Evaluate(TimeOfDay);

        [FoldoutGroup("TAB2/POST PROCESSES/FOG", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/FOG/G4"), PropertyOrder(32)]
        [SerializeField, HideLabel, LabelWidth(160)]
        Gradient fogAlbedoGradient = new() {
            alphaKeys = new[] {
                new GradientAlphaKey(1, 0f),
                new GradientAlphaKey(1, 1f)
            },
            colorKeys = new[] {
                new GradientColorKey(new Color(0.7058824f, 0.7860422f, 1f), 0f),
                new GradientColorKey(new Color(1, 1, 1), 0.5f),
                new GradientColorKey(new Color(0.7058824f, 0.7860422f, 1f), 0f)
            }
        };

        // --- Sky ---
        [FoldoutGroup("TAB2/POST PROCESSES/SKY", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/SKY/G1"), PropertyOrder(33)]
        [ShowInInspector, LabelWidth(160)]
        float SkyBlend => skyBlendCurve.Evaluate(TimeOfDay);
        
        [FoldoutGroup("TAB2/POST PROCESSES/SKY", expanded:true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/SKY/G1"), PropertyOrder(34)]
        [SerializeField, HideLabel]
        public AnimationCurve skyBlendCurve = AnimationCurve.Linear(0, 0, 1, 0);
        
        [FoldoutGroup("TAB2/POST PROCESSES/SKY", expanded:true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/SKY/G2"), PropertyOrder(35)]
        [ShowInInspector, LabelWidth(160)]
        int SkyMultiplier => (int)skyMultiplierCurve.Evaluate(TimeOfDay);
        
        [FoldoutGroup("TAB2/POST PROCESSES/SKY", expanded:true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/SKY/G2"), PropertyOrder(36)]
        [SerializeField, HideLabel]
        public AnimationCurve skyMultiplierCurve = AnimationCurve.Linear(0, 10000, 1, 10000);
        
        [FoldoutGroup("TAB2/POST PROCESSES/SKY", expanded:true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/SKY/G4"), PropertyOrder(37)]
        [SerializeField, LabelWidth(160), Range(0,360)]
        public float skyRotation;
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G1"), PropertyOrder(38)]
        [ShowInInspector, LabelWidth(160)]
        float cloudLayerOpacity => cloudLayerOpacityCurve.Evaluate(TimeOfDay);
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded:true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G1"), PropertyOrder(39)]
        [SerializeField, HideLabel]
        public AnimationCurve cloudLayerOpacityCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G2"), PropertyOrder(40)]
        [ShowInInspector, LabelWidth(160)]
        Color cloudLayerATint => cloudLayerATintGradient.Evaluate(TimeOfDay);
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G2"), PropertyOrder(41)]
        [SerializeField, HideLabel, LabelWidth(160)]
        Gradient cloudLayerATintGradient = new() {
            alphaKeys = new[] {
                new GradientAlphaKey(1, 0f),
                new GradientAlphaKey(1, 1f)
            },
            colorKeys = new[] {
                new GradientColorKey(new Color(1, 1, 1), 0f),
                new GradientColorKey(new Color(1, 1, 1), 0f)
            }
        };

        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G3"), PropertyOrder(42)]
        [ShowInInspector, LabelWidth(160)]
        float cloudLayerAExposure => cloudLayerAExposureCurve.Evaluate(TimeOfDay);
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded:true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G3"), PropertyOrder(43)]
        [SerializeField, HideLabel]
        public AnimationCurve cloudLayerAExposureCurve = AnimationCurve.Linear(0, 0, 1, 0);
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G4"), PropertyOrder(44)]
        [ShowInInspector, LabelWidth(160)]
        Color cloudLayerBTint => cloudLayerBTintGradient.Evaluate(TimeOfDay);
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G4"), PropertyOrder(45)]
        [SerializeField, HideLabel, LabelWidth(160)]
        Gradient cloudLayerBTintGradient = new() {
            alphaKeys = new[] {
                new GradientAlphaKey(1, 0f),
                new GradientAlphaKey(1, 1f)
            },
            colorKeys = new[] {
                new GradientColorKey(new Color(1, 1, 1), 0f),
                new GradientColorKey(new Color(1, 1, 1), 0f)
            }
        };
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded: true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G5"), PropertyOrder(46)]
        [ShowInInspector, LabelWidth(160)]
        float cloudLayerBExposure => cloudLayerBExposureCurve.Evaluate(TimeOfDay);
        
        [FoldoutGroup("TAB2/POST PROCESSES/CLOUD LAYER", expanded:true)]
        [TabGroup("TAB2", "POST PROCESSES", SdfIconType.CircleHalf)]
        [HorizontalGroup("TAB2/POST PROCESSES/CLOUD LAYER/G5"), PropertyOrder(47)]
        [SerializeField, HideLabel]
        public AnimationCurve cloudLayerBExposureCurve = AnimationCurve.Linear(0, 0, 1, 0);
        
        #endregion

        #region References

        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160), Required]
        DayNightSun directionalLightObject;
        
        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160), Required]
        DayNightShadowCaster shadowCasterLightObject;

        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160)]
        Volume globalVolume;

        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160)]
        Material customSkybox;
        
        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, Required]
        WyrdnightSkyboxController wyrdnightSkyboxController;
        
        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160)]
        [OnValueChanged("EDITOR_ReloadSystem")]
        TemporaryMaterialTextureLoader<Cubemap> skyDayTexture;

        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160)]
        [OnValueChanged("EDITOR_ReloadSystem")]
        TemporaryMaterialTextureLoader<Cubemap> skyNightTexture;

        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160)]
        [OnValueChanged("EDITOR_ReloadSystem")]
        TemporaryMaterialTextureLoader<Cubemap> skyWyrdnessTexture;

        [TabGroup("TAB3", "REFERENCES", SdfIconType.ArrowBarDown)]
        [SerializeField, LabelWidth(160)]
        [OnValueChanged("EDITOR_ReloadSystem")]
        TemporaryMaterialTextureLoader<Cubemap> skyWyrdnessMaskTexture;
        #endregion

        public Material SkyboxInstance => _skyboxInstance;

        #region Local properties

        // Lights
        LightWithOverride _sunLight;
        LightWithOverride _moonLight;
        LightWithOverride _shadowCasterLight;
        Transform _sunTransform;
        Transform _shadowCasterTransform;
        
        LightWithOverride _shadowCasterOwnerLight;
        Transform _shadowCasterOwnerTransform;
        Texture _shadowCasterCookieCachedTexture;
        bool _shadowCasterDayState;

        LensFlareComponentSRP _lensFlare;
        VolumeProfile _globalVolumeProfile;

        // Volume components
        Fog _fog;
        Exposure _exposure;
        // ColorCurves _colorCurves;
        ColorAdjustments _colorAdjustments;
        IndirectLightingController _indirectLightingController;
        CloudLayer _cloudLayer;
        Material _skyboxInstance;

        // Hero
        Vector3? HeroPosition => Hero.Current?.Coords;

        // Label
        Coroutine _labelCoroutine;
        float _labelAlpha;

        #endregion

        // ---------
        void OnEnable() {
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving += EDITOR_SceneSavingCallback;
#endif
            Init();
        }

        void OnDisable() {
#if UNITY_EDITOR
            EditorSceneManager.sceneSaving -= EDITOR_SceneSavingCallback;
            if (!Application.isPlaying) {
                if (_skyboxInstance != null) {
                    GameObjects.DestroySafely(_skyboxInstance);
                }
            }
#endif
            UnloadSkyTextures();
        }

        protected override void OnDestroy() {
            if (_skyboxInstance != null) {
                GameObjects.DestroySafely(_skyboxInstance);
            }

            base.OnDestroy();
        }

        void Init() {
            if (Application.isBatchMode) {
                return;
            }

            _sunLight = directionalLightObject.GetOrAddComponent<LightWithOverride>();
            _sunTransform = _sunLight.transform;

            var moon = directionalLightObject.transform.parent.GetComponentInChildren<DayNightMoon>();
            if (moon != null) {
                _moonLight = moon.GetOrAddComponent<LightWithOverride>();
            } else {
                _moonLight = null;
            }
            
            InitializeShadowCaster();

            _lensFlare = directionalLightObject.GetComponent<LensFlareComponentSRP>();
            _globalVolumeProfile = globalVolume.GetSharedOrInstancedProfile();

            if (_sunLight == null ||
                _lensFlare == null ||
                globalVolume == null ||
                _globalVolumeProfile == null) {
                enabled = false;
            }

            if (!_globalVolumeProfile.TryGet(out _exposure)) {
                Debug.LogError("Fog not found in global volume profile.");
                enabled = false;
            }

            if (!_globalVolumeProfile.TryGet(out _fog)) {
                Debug.LogError("Fog not found in global volume profile.");
                enabled = false;
            }

            // if (!_globalVolumeProfile.TryGet(out _colorCurves)) {
            //     _colorCurves = _globalVolumeProfile.Add<ColorCurves>(true);
            //
            //     if (_colorCurves == null) {
            //         Debug.LogError("Failed to add ColorCurves to Volume Profile.");
            //         enabled = false;
            //     }
            // }

            if (!_globalVolumeProfile.TryGet(out _colorAdjustments)) {
                Debug.LogError("Color Adjustments not found in global volume profile.");
                enabled = false;
            }

            if (!_globalVolumeProfile.TryGet(out _cloudLayer)) {
                Debug.LogError("Cloud Layer not found in global volume profile.");
                enabled = false;
            }

            if (!_globalVolumeProfile.TryGet(out _indirectLightingController)) {
                Debug.LogError("IndirectLightingController not found in global volume profile.");
                enabled = false;
            }

            if (customSkybox == null) {
                Debug.LogError("Custom skybox material not assigned!");
            } else if (_skyboxInstance == null) {
                _skyboxInstance = new Material(customSkybox);
                if (_globalVolumeProfile.TryGet(out PhysicallyBasedSky sky)) {
                    sky.material.value = _skyboxInstance;
                }
            }

            InitializeSkyTexturesLoaders();
        }

        void OnValidate() {
#if UNITY_EDITOR
            EDITOR_ReloadSystem();
#endif
            // areCurvesValid = colorCurveDay.length == colorCurveNight.length;
        }

        void Update() {
#if UNITY_EDITOR
            if (!Application.isPlaying && !LightController.EditorPreviewUpdates) {
                return;
            }
#endif
            HandleTimeChange();
            HandleLight();
            HandleLightsRotation();
            HandleFog();
            HandleLensFlare();
            HandleExposure();
            HandlePhysicallyBasedSKy();
            HandleIndirectLighting();
            HandleColorCorrection();
            HandleCloudLayer();
            // HandleColorCurves();
            HandleShadowCasterLight();
            HandleShadowCasterRotation();
            HandleShadowCasterRelativePositioning();
            KeyboardControl();
#if UNITY_EDITOR
            if (Application.isPlaying) {
                HandleLoadingSkyboxMaterialTextures();
            }
#else
            HandleLoadingSkyboxMaterialTextures();
#endif
        }
#if UNITY_EDITOR
        void EDITOR_SceneSavingCallback(UnityEngine.SceneManagement.Scene scene, string path) {
            if (_globalVolumeProfile == null) {
                return;
            }

            EditorUtility.SetDirty(_globalVolumeProfile);
        }
#endif

        void HandleTimeChange() {
            float minutes = TimeOfDay * TotalMinutesInDay;

            _hour = (int)(minutes / 60);
            _minute = (int)(minutes % 60);

            if (_hour >= 24) {
                _hour = 23;
                _minute = 59;
                _editorTimeOfDay = 1.0f;
            }
        }

        void KeyboardControl() {
            if (!CheatController.CheatsEnabled()) {
                return;
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals)) {
                Target.WeatherIncrementDayFloat(0.02f);
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus)) {
                if (TimeOfDay < 0.1) {
                    Target.WeatherIncrementDayFloat(1f - TimeOfDay);
                }

                Target.WeatherIncrementDayFloat(-0.02f);
            }
        }

        void HandleLight() {
            // Intensity
            var intensity = lightIntensityCurve.Evaluate(TimeOfDay);
            _sunLight.intensity = intensity;
            _moonLight.intensity = intensity;

            var lightDimmer = lightIntensityMultiplierCurve.Evaluate(TimeOfDay);
            _sunLight.lightDimmer = lightDimmer;
            _moonLight.lightDimmer = lightDimmer;
            _shadowCasterLight.lightDimmer = lightDimmer;

            // Temperature
            var colorTemperature = lightTemperatureCurve.Evaluate(TimeOfDay);
            _sunLight.colorTemperature = colorTemperature;
            _moonLight.colorTemperature = colorTemperature;
            _shadowCasterLight.colorTemperature = colorTemperature;

            // Color
            if (enableLightTint) {
                Color lightTint = lightTintGradient.Evaluate(TimeOfDay);
                _sunLight.color = lightTint;
                _moonLight.color = lightTint;
                _shadowCasterLight.color = lightTint;
            } else {
                _sunLight.color = Color.white;
                _moonLight.color = Color.white;
                _shadowCasterLight.color = Color.white;
            }

            // Moon Surface Tint
            _moonLight.surfaceTint = moonSurfaceTintGradient.Evaluate(TimeOfDay);

            // Volumetric
            var volumetricDimmer = lightVolumetricMultiplierCurve.Evaluate(TimeOfDay);
            _sunLight.volumetricDimmer = volumetricDimmer;
            _moonLight.volumetricDimmer = volumetricDimmer;
            _shadowCasterLight.volumetricDimmer = volumetricDimmer;

            // Flare Multiplier
            _sunLight.flareSize = sunFlareSizeCurve.Evaluate(TimeOfDay);
            _sunLight.flareMultiplier = sunFlareMultiplierCurve.Evaluate(TimeOfDay);
        }

        void HandleLightsRotation() {
            float lightsRotationX;
            
            if (TimeOfDay is >= ARDateTime.NightEnd and <= ARDateTime.NightStart) {
                float dayProgress = Mathf.InverseLerp(ARDateTime.NightEnd, ARDateTime.NightStart, TimeOfDay);
                lightsRotationX = Mathf.Lerp(0, 180, dayProgress);
            } else {
                float nightDuration = ARDateTime.NightEnd - ARDateTime.NightStart + 1f;
                float nightProgress = TimeOfDay > ARDateTime.NightStart
                    ? (TimeOfDay - ARDateTime.NightStart) / nightDuration
                    : (TimeOfDay - ARDateTime.NightStart + 1f) / nightDuration;

                lightsRotationX = Mathf.Lerp(180, 360, nightProgress);
            }

            var inclinationAngle = Quaternion.Euler(0, inclination, 0);
            var lightsRotation = Quaternion.Euler(lightsRotationX, HorizontalAngle, 0) * inclinationAngle;
            _sunTransform.rotation = lightsRotation;
        }

        void HandleFog() {
            _fog.meanFreePath.value = fogDensityCurve.Evaluate(TimeOfDay);
            _fog.albedo.value = fogAlbedoGradient.Evaluate(TimeOfDay);
            _fog.anisotropy.value = fogAnisotropyCurve.Evaluate(TimeOfDay);

            var referenceHeight = GetPointOfViewReferencePosition()?.y;
            _fog.baseHeight.value = referenceHeight ?? 0f;
            _fog.maximumHeight.value = fogHeightCurve.Evaluate(TimeOfDay) + (referenceHeight ?? 400f);
        }

        Vector3? GetPointOfViewReferencePosition() {
            if (Application.isPlaying) {
                return HeroPosition;
            }
#if UNITY_EDITOR
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null) {
                return SceneView.lastActiveSceneView.camera.transform.position;
            }
#endif
            return null;
        }

        void HandlePhysicallyBasedSKy() {
            _skyboxInstance.SetFloat("_SkyBlend", skyBlendCurve.Evaluate(TimeOfDay));
            _skyboxInstance.SetFloat("_SkyEmissionMultiplier", skyMultiplierCurve.Evaluate(TimeOfDay));
            _skyboxInstance.SetFloat("_SkyRotation", TimeOfDay * -360f + skyRotation);
        }

        // void HandleColorCurves() {
        //     var masterTextureCurve = _colorCurves.master.value;

            //AnimationCurve currentColorCurve = LerpCurves(colorCurveNight, colorCurveDay, TimeOfDay);
            //AnimationCurveToTextureCurve(masterTextureCurve, currentColorCurve);
        // }

        void HandleExposure() {
            switch (exposureType) {
                case ExposureType.Automatic:
                    _exposure.mode.value = ExposureMode.AutomaticHistogram;
                    _exposure.limitMin.value = exposureLimitMinCurve.Evaluate(TimeOfDay);
                    _exposure.limitMax.value = exposureLimitMaxCurve.Evaluate(TimeOfDay);
                    _exposure.histogramPercentages.value = new Vector2(10, 70);
                    _exposure.adaptationSpeedDarkToLight.value = 10f;
                    _exposure.adaptationSpeedLightToDark.value = 10f;
                    _exposure.histogramPercentages.value = new Vector2(exposureHistogramMinCurve.Evaluate(TimeOfDay),
                        exposureHistogramMaxCurve.Evaluate(TimeOfDay));
                    _exposure.compensation.value = exposureCompensationCurve.Evaluate(TimeOfDay);
                    break;
                case ExposureType.Fixed:
                    _exposure.mode.value = ExposureMode.Fixed;
                    _exposure.fixedExposure.value = exposureCurve.Evaluate(TimeOfDay);
                    break;
                case ExposureType.PhysicalCamera:
                    _exposure.mode.value = ExposureMode.UsePhysicalCamera;
                    _exposure.compensation.value = exposureCompensationCurve.Evaluate(TimeOfDay);
                    break;
            }
        }

        void HandleColorCorrection() {
            _colorAdjustments.colorFilter.value = colorFilterGradient.Evaluate(TimeOfDay);
            _colorAdjustments.postExposure.value = postExposureCurve.Evaluate(TimeOfDay);
        }

        void HandleCloudLayer() {
            float t = Target?.Element<WeatherController>().PrecipitationIntensity ?? 0.0f;
            float time = TimeOfDay;

            float opacityBase = cloudLayerOpacityCurve.Evaluate(time);
            float exposureA = cloudLayerAExposureCurve.Evaluate(time);
            float exposureB = cloudLayerBExposureCurve.Evaluate(time);

            Color tintA = cloudLayerATintGradient.Evaluate(time);
            Color tintB = cloudLayerBTintGradient.Evaluate(time);

            _cloudLayer.opacity.value = math.lerp(opacityBase, 1.0f, t);

            _cloudLayer.layerA.tint.value = tintA;
            _cloudLayer.layerA.exposure.value = math.lerp(exposureA, -1.0f, t);

            float4 layerAOpacityBase = new float4(1.0f, 0.2f, 0.2f, 0.0f); // R, G, B, A
            float4 layerAOpacityPrecipitation   = new float4(0.0f, 0.0f, 0.0f, 0.2f);
            float4 layerAOpacity      = math.lerp(layerAOpacityBase, layerAOpacityPrecipitation, t);

            _cloudLayer.layerA.opacityR.value = layerAOpacity.x;
            _cloudLayer.layerA.opacityG.value = layerAOpacity.y;
            _cloudLayer.layerA.opacityB.value = layerAOpacity.z;
            _cloudLayer.layerA.opacityA.value = layerAOpacity.w;

            _cloudLayer.layerB.tint.value = tintB;
            _cloudLayer.layerB.exposure.value = math.lerp(exposureB, -1.0f, t);

            float4 layerBOpacityBase = new float4(0.0f, 0.0f, 0.0f, 0.0f);
            float4 layerBOpacityPrecipitation   = new float4(0.1f, 0.0f, 0.0f, 0.1f);
            float4 layerBOpacity      = math.lerp(layerBOpacityBase, layerBOpacityPrecipitation, t);

            _cloudLayer.layerB.opacityR.value = layerBOpacity.x;
            _cloudLayer.layerB.opacityG.value = layerBOpacity.y;
            _cloudLayer.layerB.opacityB.value = layerBOpacity.z;
            _cloudLayer.layerB.opacityA.value = layerBOpacity.w;
        }

        void HandleIndirectLighting() {
            _indirectLightingController.indirectDiffuseLightingMultiplier.value = indirectDiffuseCurve.Evaluate(TimeOfDay);
        }

        void InitializeShadowCaster() {
            _shadowCasterLight = shadowCasterLightObject.GetOrAddComponent<LightWithOverride>();
            _shadowCasterTransform = _shadowCasterLight.transform;
            
            _sunLight.shadows = LightShadows.None;
            _moonLight.shadows = LightShadows.None;
            _shadowCasterLight.shadows = LightShadows.Soft;
        }
        
        void HandleShadowCasterLight() {
            UpdateShadowCasterOwner();

            _shadowCasterLight.intensity = _shadowCasterOwnerLight.intensity * CalculateShadowIntensityMultiplier();
            
            _shadowCasterLight.surfaceTint = _shadowCasterOwnerLight.surfaceTint;
            _shadowCasterLight.flareSize = _shadowCasterOwnerLight.flareSize;
            _shadowCasterLight.flareMultiplier = _shadowCasterOwnerLight.flareMultiplier;
        }

        void UpdateShadowCasterOwner() {
            bool isDay = TimeOfDay is >= ARDateTime.NightEnd and <= ARDateTime.NightStart;
            if (isDay == _shadowCasterDayState && _shadowCasterOwnerLight != null) {
                return;
            }
            _shadowCasterDayState = isDay;
            
            if (isDay) {
                _shadowCasterOwnerLight = _sunLight;
                _shadowCasterOwnerTransform = _sunTransform;
                if (_shadowCasterCookieCachedTexture != null) {
                    _shadowCasterLight.cookie = _shadowCasterCookieCachedTexture;
                    _shadowCasterCookieCachedTexture = null;
                }
            } else {
                _shadowCasterOwnerLight = _moonLight;
                _shadowCasterOwnerTransform = _moonLight.transform;
                if (_shadowCasterLight.cookie != null && disableShadowCookiesAtNight) {
                    _shadowCasterCookieCachedTexture = _shadowCasterLight.cookie;
                    _shadowCasterLight.cookie = null;
                }
            }
        }

        float CalculateShadowIntensityMultiplier() {
            float currentMinutes = TimeOfDay * TotalMinutesInDay;
            float nightEndMinutes = ARDateTime.NightEnd * TotalMinutesInDay;
            float nightStartMinutes = ARDateTime.NightStart * TotalMinutesInDay;
            
            float minutesFromNightEnd = math.abs(currentMinutes - nightEndMinutes);
            float minutesFromNightStart = math.abs(currentMinutes - nightStartMinutes);

            float circularNightEndDistance = math.min(minutesFromNightEnd, TotalMinutesInDay - minutesFromNightEnd);
            float circularNightStartDistance = math.min(minutesFromNightStart, TotalMinutesInDay - minutesFromNightStart);

            float minutesFromSwitching = math.min(circularNightEndDistance, circularNightStartDistance);
            return math.min(minutesFromSwitching / shadowIntensityBlendMinutes, 1f);
        }
        
        void HandleShadowCasterRotation() {
            var currentForward = _shadowCasterOwnerTransform.forward;

            var shadowHorizontalDirection = currentForward.ToHorizontal3().normalized;
            
            var currentShadowHeight = math.abs(currentForward.y);
            var minimumShadowHeight = math.sin(shadowMinimumAngle * math.TORADIANS);
            var maximumShadowHeight = math.cos(inclination * math.TORADIANS);
            
            var clampedShadowHeight = math.max(currentShadowHeight, minimumShadowHeight);
            var smoothShadowHeight = math.lerp(minimumShadowHeight, maximumShadowHeight, currentShadowHeight);
            var newShadowHeight = math.lerp(smoothShadowHeight, clampedShadowHeight, shadowAngleSnapFactor);
            
            var newShadowLenght = math.sqrt(1f - newShadowHeight * newShadowHeight);
            var verticalComponent = Vector3.down * newShadowHeight;
            var horizontalComponent = shadowHorizontalDirection * newShadowLenght;
            var shadowCasterDirection = horizontalComponent + verticalComponent;

            _shadowCasterTransform.rotation = Quaternion.LookRotation(shadowCasterDirection, _shadowCasterOwnerTransform.up);
        }

        void HandleShadowCasterRelativePositioning() {
            // Shadow caster is used to project cookies on the world. We want to keep it close to the point of view
            // while preventing it from visually moving. This is done by positioning the light on the same relative
            // position on the cookie grid.
            
            if (!shadowCasterFollowPoV) {
                return;
            }
            
            var rotationPivot = GetPointOfViewReferencePosition();
            if (!rotationPivot.HasValue) {
                _shadowCasterTransform.localPosition = Vector3.zero;
                return;
            }
            
            var deltaToRotationPivot = _shadowCasterTransform.position - rotationPivot.Value;
            
            var up = _shadowCasterTransform.up;
            var right = _shadowCasterTransform.right;
            var deltaAlongLocalX = right * (Vector3.Dot(deltaToRotationPivot, right) % _shadowCasterLight.shapeWidth);
            var deltaAlongLocalY = up * (Vector3.Dot(deltaToRotationPivot, up) % _shadowCasterLight.shapeHeight);
            
            _shadowCasterTransform.position = rotationPivot.Value + deltaAlongLocalX + deltaAlongLocalY;
        }

        void HandleLensFlare() {
            if (enableLensFlare) {
                _lensFlare.enabled = enableLensFlare;
                _lensFlare.intensity = lightLensFlare.Evaluate(TimeOfDay);
            } else {
                _lensFlare.enabled = false;
            }
        }

        void HandleLoadingSkyboxMaterialTextures() {
            //TODO commented out have stable sky textures in build (textures are directly attached to material)
            /*skyDayTexture.Update();
            skyNightTexture.Update();
            skyWyrdnessTexture.Update();
            skyWyrdnessMaskTexture.Update();*/
        }

        void InitializeSkyTexturesLoaders() {
            //TODO commented out have stable sky textures in build (textures are directly attached to material)
            /*skyDayTexture.Init(ShouldBeLoadedEmissionTextureDay, ShouldBeUnloadedEmissionTextureDay, _skyboxInstance, Shader.PropertyToID(SkyDayMapProp));
            skyNightTexture.Init(ShouldBeLoadedEmissionTextureNight, ShouldBeUnloadedEmissionTextureNight, _skyboxInstance, Shader.PropertyToID(SkyNightMap));
            skyWyrdnessTexture.Init(ShouldBeLoadedWyrdnessTexture, ShouldBeUnloadedWyrdnessTexture, _skyboxInstance, Shader.PropertyToID(SkyWyrdnessMap));
            skyWyrdnessMaskTexture.Init(ShouldBeLoadedWyrdnessTexture, ShouldBeUnloadedWyrdnessTexture, _skyboxInstance, Shader.PropertyToID(SkyWyrdnessMaskMap));
            
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                if (skyDayTexture.HasAssetRef) {
                    skyDayTexture.LoadTextureAsync();
                }
                if (skyNightTexture.HasAssetRef) {
                    skyNightTexture.LoadTextureAsync();
                }
                if (skyWyrdnessTexture.HasAssetRef) {
                    skyWyrdnessTexture.LoadTextureAsync();
                }
                if (skyWyrdnessMaskTexture.HasAssetRef) {
                    skyWyrdnessMaskTexture.LoadTextureAsync();
                }
            }
#endif*/
        }
        
        void UnloadSkyTextures() {
            //TODO commented out have stable sky textures in build (textures are directly attached to material)
            /*skyDayTexture.UnloadTexture();
            skyNightTexture.UnloadTexture();
            skyWyrdnessTexture.UnloadTexture();
            skyWyrdnessMaskTexture.UnloadTexture();*/
        }
        
        bool ShouldBeLoadedEmissionTextureDay() => WillUseEmissionTextureDay(skyBlendCurve.Evaluate((TimeOfDay + 0.1f) % 1));
        bool ShouldBeUnloadedEmissionTextureDay() => WillUseEmissionTextureDay(skyBlendCurve.Evaluate(TimeOfDay)) == false;
        bool ShouldBeLoadedEmissionTextureNight() => WillUseEmissionTextureNight(skyBlendCurve.Evaluate((TimeOfDay + 0.1f) % 1));
        bool ShouldBeUnloadedEmissionTextureNight() => WillUseEmissionTextureNight(skyBlendCurve.Evaluate(TimeOfDay)) == false;
        bool ShouldBeLoadedWyrdnessTexture() => WillUseWyrdnessTexture(wyrdnightSkyboxController.CurrentValue);
        bool ShouldBeUnloadedWyrdnessTexture() => WillUseWyrdnessTexture(wyrdnightSkyboxController.CurrentValue) == false;

        static bool WillUseEmissionTextureDay(float spaceBlendValue) => spaceBlendValue < 1;
        static bool WillUseEmissionTextureNight(float spaceBlendValue) => spaceBlendValue > 0;
        static bool WillUseWyrdnessTexture(float wyrdnessControllerValue) => wyrdnessControllerValue > 0;
        
        [UnityEngine.Scripting.Preserve]
        static AnimationCurve LerpCurves(AnimationCurve curveA, AnimationCurve curveB, float timeOfDay) {
            var lerpedCurve = new AnimationCurve();

            if (curveA != null && curveB != null) {
                int keyCount = Mathf.Min(curveA.keys.Length, curveB.keys.Length);

                float t = Mathf.Sin(timeOfDay * Mathf.PI);

                for (int i = 0; i < keyCount; i++) {
                    Keyframe dayKey = curveA.keys[i];
                    Keyframe nightKey = curveB.keys[i];

                    float time = Mathf.Lerp(dayKey.time, nightKey.time, t);
                    float value = Mathf.Lerp(dayKey.value, nightKey.value, t);

                    float inTangent = Mathf.Lerp(dayKey.inTangent, nightKey.inTangent, t);
                    float outTangent = Mathf.Lerp(dayKey.outTangent, nightKey.outTangent, t);

                    lerpedCurve.AddKey(new Keyframe(time, value, inTangent, outTangent));
                }
            }

            return lerpedCurve;
        }

        [UnityEngine.Scripting.Preserve]
        void AnimationCurveToTextureCurve(TextureCurve textureCurve, AnimationCurve animationCurve) {
            const int SamplesCount = 32;

            int numKeys = textureCurve.length;
            for (int i = numKeys - 1; i >= 0; i--) {
                textureCurve.RemoveKey(i);
            }

            float minTime = animationCurve.keys[0].time;
            float maxTime = animationCurve.keys[animationCurve.length - 1].time;
            float step = (maxTime - minTime) / (SamplesCount - 1);

            for (int i = 0; i < SamplesCount; i++) {
                float time = minTime + i * step;
                float value = animationCurve.Evaluate(time);

                Keyframe evaluatedKey = animationCurve.keys[i % animationCurve.length];
                float inTangent = evaluatedKey.inTangent;
                float outTangent = evaluatedKey.outTangent;

                int keyIndex = textureCurve.AddKey(time, value);
                if (keyIndex >= 0) {
                    textureCurve.MoveKey(keyIndex, new Keyframe(time, value, inTangent, outTangent));
                }
            }

            textureCurve.SetDirty();
        }

        enum ExposureType : byte {
            Automatic = 0,
            Fixed = 1,
            PhysicalCamera = 2
        }

        // === Editor
        // -- Odin
        Color ODIN_GetProgressBarColor() {
            Vector4 colorVector = Mathf.CorrelatedColorTemperatureToRGB(LightTemperature);
            return colorVector;
        }

        bool ODIN_IsAutomaticExposure() {
            return exposureType == ExposureType.Automatic;
        }

        bool ODIN_IsFixedExposure() {
            return exposureType == ExposureType.Fixed;
        }

        bool ODIN_IsPhysicalCamera() {
            return exposureType == ExposureType.PhysicalCamera;
        }

        bool ODIN_IsFixedOrPhysicalCamera() {
            return ODIN_IsFixedExposure() || ODIN_IsPhysicalCamera();
        }

#if UNITY_EDITOR
        [FoldoutGroup("TAB1/TIME/SET TIME", expanded: true)]
        [TabGroup("TAB1", "TIME", SdfIconType.AlarmFill)]
        [SerializeField, MinValue(0), MaxValue(23), LabelText("Set Hour")]
        int EDITOR_setHour = 12;

        [FoldoutGroup("TAB1/TIME/SET TIME", expanded: true)]
        [TabGroup("TAB1", "TIME", SdfIconType.AlarmFill)]
        [SerializeField, MinValue(0), MaxValue(59), LabelText("Set Minute")]
        int EDITOR_setMinute;

        [FoldoutGroup("TAB1/TIME/SET TIME", expanded: true)]
        [TabGroup("TAB1", "TIME", SdfIconType.AlarmFill)]
        [Button("SET TIME", ButtonSizes.Medium)]
        void EDITOR_SetTime() {
            float totalMinutes = (EDITOR_setHour % 24) * 60.0f + (EDITOR_setMinute % 60);
            _editorTimeOfDay = totalMinutes / TotalMinutesInDay;
            _editorTimeOfDay = Mathf.Clamp01(_editorTimeOfDay);
            HandleTimeChange();
        }

        [FoldoutGroup("TAB1/TIME/CYCLE", expanded: true)]
        [TabGroup("TAB1", "TIME", SdfIconType.ArrowClockwise)]
        [SerializeField, MinValue(0.1), MaxValue(60), LabelText("CYCLE TIME")]
        float EDITOR_CycleTime = 2f;

        bool EDITOR_IsCycling;

        [FoldoutGroup("TAB1/TIME/CYCLE", expanded: true)]
        [TabGroup("TAB1", "TIME", SdfIconType.ArrowClockwise)]
        [Button("START/STOP CYCLE", ButtonSizes.Medium), GUIColor(nameof(EDITOR_GetButtonColor))]
        public void EDITOR_ToggleCycle() {
            if (!EDITOR_IsCycling) {
                StartCoroutine(EDITOR_StartCycle());
            } else {
                StopAllCoroutines();
                EDITOR_IsCycling = false;
            }
        }

        Color EDITOR_GetButtonColor() {
            return this.EDITOR_IsCycling ? Color.red : Color.green;
        }

        [TabGroup("TAB3", "SAVE SETTINGS", SdfIconType.Save, order: 999)]
        [SerializeField, AssetSelector(Paths = "Assets/3DAssets/Lighting/DayNightSystem/SaveData"), LabelText("JSON File"), LabelWidth(160)]
        Object EDITOR_jsonFile;

        [TabGroup("TAB3", "SAVE SETTINGS", SdfIconType.Save, order: 999), SerializeField, LabelText("Save file name"), LabelWidth(160)]
        string EDITOR_saveFileName = "DayNightSystem_Save.json";

        [TabGroup("TAB3", "SAVE SETTINGS", SdfIconType.Save)]
        [ButtonGroup("TAB3/SAVE SETTINGS/BUTTONS", ButtonHeight = 25), Button(SdfIconType.FileEarmarkCheckFill, "")]
        [Tooltip("Save to current JSON file")]
        void EDITOR_SaveSettingsJson() {
            var filePath = Path.Combine(Application.dataPath, "3DAssets/Lighting/DayNightSystem/SaveData", EDITOR_saveFileName);

            if (string.IsNullOrEmpty(filePath)) {
                Debug.LogError("File path is invalid.");
                return;
            }

            var settings = new DayNightSystemSettings(this);

            string json = JsonUtility.ToJson(settings);
            File.WriteAllText(filePath, json);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"File saved: {filePath}");
        }

        [TabGroup("TAB3", "SAVE SETTINGS", SdfIconType.Save)]
        [ButtonGroup("TAB3/SAVE SETTINGS/BUTTONS"), Button(SdfIconType.FileEarmarkArrowDownFill, "")]
        [Tooltip("Load selected JSON file")]
        void EDITOR_LoadSettings() {
            if (EDITOR_jsonFile == null) {
                return;
            }

            string filePath = UnityEditor.AssetDatabase.GetAssetPath(EDITOR_jsonFile);
            string json = File.ReadAllText(filePath);
            DayNightSystemSettings settings = JsonUtility.FromJson<DayNightSystemSettings>(json);
            settings.Apply(this);
            Debug.Log("File loaded!");
        }

        DayNightSystemComponentController[] _editorComponentControllers = Array.Empty<DayNightSystemComponentController>();
        void EDITOR_InitializeControllers() {
            _editorComponentControllers = Object.FindObjectsByType<DayNightSystemComponentController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            _editorComponentControllers.ForEach(controller => {
                controller.EDITOR_Initialize();
                controller.EDITOR_TimeOfDayChanged(_editorTimeOfDay);
            });
        }
        
        IEnumerator EDITOR_StartCycle() {
            EDITOR_IsCycling = true;
            float cycleTimeValue = 1.0f / (EDITOR_CycleTime * 60f);

            while (EDITOR_IsCycling) {
                _editorTimeOfDay += Time.deltaTime * cycleTimeValue;
                _editorTimeOfDay = Mathf.Repeat(_editorTimeOfDay, 1f);

                HandleTimeChange();

                yield return null;
            }
        }

        void EDITOR_SyncTimeParameters() {
            _editorTimeOfDay = TimeOfDay;
        }

        void EDITOR_TimeOfDayChanged(float newTime) {
            _editorTimeOfDay = newTime;
            HandleTimeChange();
            
            if (!Application.isPlaying && LightController.EditorPreviewUpdates) {
                _editorComponentControllers.ForEach(controller => controller.EDITOR_TimeOfDayChanged(newTime));
            }
        }

        void EDITOR_SyncRotationParameters() {
            _editorHorizontalAngle = horizontalAngle;
        }

        void EDITOR_ReloadSystem() {
            if (Application.isPlaying || !LightController.EditorPreviewUpdates) {
                return;
            }

            if (_skyboxInstance != null) {
                GameObjects.DestroySafely(_skyboxInstance);
            }

            Init();
            Update();
        }

        public struct EDITOR_Access {
            public DayNightSystem DayNightSystem { get; private set; }

            public bool IsValid => DayNightSystem != null;

            public float CycleValue {
                get => DayNightSystem.EDITOR_CycleTime;
                set => DayNightSystem.EDITOR_CycleTime = value;
            }

            public float EditorTimeOfDay {
                [UnityEngine.Scripting.Preserve]get => DayNightSystem._editorTimeOfDay;
                [UnityEngine.Scripting.Preserve] set => DayNightSystem._editorTimeOfDay = value;
            }

            public float EditorHorizontalAngle {
                [UnityEngine.Scripting.Preserve] get => DayNightSystem._editorHorizontalAngle;
                [UnityEngine.Scripting.Preserve] set => DayNightSystem._editorHorizontalAngle = value;
            }

            public void Initialize(DayNightSystem dayNightSystem) {
                DayNightSystem = dayNightSystem;
                DayNightSystem?.EDITOR_InitializeControllers();
                DayNightSystem?.EDITOR_ReloadSystem();
            }

            public void TimeOfDayChanged(float newTime) {
                DayNightSystem?.EDITOR_TimeOfDayChanged(newTime);
            }
        }

        void OnGUI() {
            if (!Application.isPlaying) {
                return;
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals) ||
                Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus)) {
                if (_labelCoroutine != null) {
                    StopCoroutine(_labelCoroutine);
                }

                _labelCoroutine = StartCoroutine(LabelFadeInAndOut(1f, 2f));
            }

            var labelStyle = new GUIStyle(EditorStyles.label) {
                fontSize = 24
            };

            float screenX = Screen.width * 0.5f - 100f;
            float screenY = Screen.height * 0.75f - 24f;

            GUILayout.BeginArea(new Rect(screenX, screenY, 200f, 48f));
            GUI.contentColor = new Color(1f, 1f, 1f, _labelAlpha);
            GUILayout.Label($"TIME: {_hour:00}:{_minute:00}", labelStyle);

            GUILayout.EndArea();
        }

        IEnumerator LabelFadeInAndOut(float fadeInSeconds, float fadeOutSeconds) {
            yield return LabelFadeIn(fadeInSeconds);
            yield return new WaitForSeconds(1f);
            yield return LabelFadeOut(fadeOutSeconds);
        }

        IEnumerator LabelFadeIn(float seconds) {
            float elapsedTime = 0f;

            while (elapsedTime < seconds) {
                _labelAlpha = Mathf.Lerp(0f, 1f, elapsedTime / seconds);
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            _labelAlpha = 1f;
        }

        IEnumerator LabelFadeOut(float seconds) {
            float elapsedTime = 0f;

            while (elapsedTime < seconds) {
                _labelAlpha = Mathf.Lerp(1f, 0f, elapsedTime / seconds);
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            _labelAlpha = 0f;
        }

        [System.Serializable]
        class DayNightSystemSettings {
            // Time
            public float editorTimeOfDay;

            // Light Angle
            public float horizontalAngle;
            public float inclination;

            // Light Intensity
            public SerializableAnimationCurve lightIntensityCurve;
            public SerializableAnimationCurve lightIntensityMultiplierCurve;
            public SerializableAnimationCurve lightVolumetricMultiplierCurve;

            // Light Color
            public SerializableAnimationCurve lightTemperatureCurve;
            public SerializableGradient colorFilterGradient;
            public SerializableGradient moonSurfaceTintGradient;
            public SerializableGradient lightTintGradient;
            public bool enableLightTint;

            // Shadows
            // public SerializableGradient shadowsTintGradient;

            // Lens Flare
            public bool enableLensFlare;
            public SerializableAnimationCurve lightLensFlare;

            // Exposure
            public SerializableAnimationCurve exposureCurve;
            public SerializableAnimationCurve exposureLimitMinCurve;
            public SerializableAnimationCurve exposureLimitMaxCurve;
            public SerializableAnimationCurve exposureHistogramPercentagesMin;
            public SerializableAnimationCurve exposureHistogramPercentagesMax;

            // Post Exposure
            public SerializableAnimationCurve postExposureCurve;
            public SerializableAnimationCurve indirectDiffuseCurve;

            // Fog
            public SerializableAnimationCurve fogDensityCurve;
            public SerializableAnimationCurve fogHeightCurve;
            public SerializableAnimationCurve fogAnisotropyCurve;

            // Sky
            public SerializableAnimationCurve spaceBlendCurve;
            public SerializableAnimationCurve spaceMultiplier;
            public float spaceRotation;
            public float spaceDistortionIntensity;
            public float spaceDistortionScale;
            public float spaceDistortionSpeed;

            public SerializableAnimationCurve cloudLayerOpacityCurve;
            public SerializableGradient cloudLayerATintGradient;
            public SerializableAnimationCurve cloudLayerAExposureCurve;
            public SerializableGradient cloudLayerBTintGradient;
            public SerializableAnimationCurve cloudLayerBExposureCurve;

            public DayNightSystemSettings(DayNightSystem dayNightSystem) {
                // Time
                editorTimeOfDay = dayNightSystem._editorTimeOfDay;

                // Light Angle
                horizontalAngle = dayNightSystem.horizontalAngle;
                inclination = dayNightSystem.inclination;

                // Light Intensity
                lightIntensityCurve = new SerializableAnimationCurve(dayNightSystem.lightIntensityCurve);
                lightIntensityMultiplierCurve =
                    new SerializableAnimationCurve(dayNightSystem.lightIntensityMultiplierCurve);
                lightVolumetricMultiplierCurve =
                    new SerializableAnimationCurve(dayNightSystem.lightVolumetricMultiplierCurve);

                // Light Color
                lightTemperatureCurve = new SerializableAnimationCurve(dayNightSystem.lightTemperatureCurve);
                colorFilterGradient = new SerializableGradient(dayNightSystem.colorFilterGradient);
                moonSurfaceTintGradient = new SerializableGradient(dayNightSystem.moonSurfaceTintGradient);
                lightTintGradient = new SerializableGradient(dayNightSystem.lightTintGradient);
                enableLightTint = dayNightSystem.enableLightTint;

                // Shadows
                // shadowsTintGradient = new SerializableGradient(dayNightSystem.shadowsTintGradient);

                // Lens Flare
                enableLensFlare = dayNightSystem.enableLensFlare;
                lightLensFlare = new SerializableAnimationCurve(dayNightSystem.lightLensFlare);

                // Exposure
                exposureCurve = new SerializableAnimationCurve(dayNightSystem.exposureCurve);
                exposureLimitMinCurve = new SerializableAnimationCurve(dayNightSystem.exposureLimitMinCurve);
                exposureLimitMaxCurve = new SerializableAnimationCurve(dayNightSystem.exposureLimitMaxCurve);
                exposureHistogramPercentagesMin = new SerializableAnimationCurve(dayNightSystem.exposureHistogramMinCurve);
                exposureHistogramPercentagesMax = new SerializableAnimationCurve(dayNightSystem.exposureHistogramMaxCurve);

                // Post Exposure
                postExposureCurve = new SerializableAnimationCurve(dayNightSystem.postExposureCurve);
                indirectDiffuseCurve = new SerializableAnimationCurve(dayNightSystem.indirectDiffuseCurve);

                // Fog
                fogDensityCurve = new SerializableAnimationCurve(dayNightSystem.fogDensityCurve);
                fogHeightCurve = new SerializableAnimationCurve(dayNightSystem.fogHeightCurve);
                fogAnisotropyCurve = new SerializableAnimationCurve(dayNightSystem.fogAnisotropyCurve);

                // Sky
                spaceBlendCurve = new SerializableAnimationCurve(dayNightSystem.skyBlendCurve);
                spaceMultiplier = new SerializableAnimationCurve(dayNightSystem.skyMultiplierCurve);
                spaceRotation = dayNightSystem.skyRotation;
                
                // Cloud Layer
                cloudLayerOpacityCurve = new SerializableAnimationCurve(dayNightSystem.cloudLayerOpacityCurve);
                cloudLayerATintGradient = new SerializableGradient(dayNightSystem.cloudLayerATintGradient);
                cloudLayerAExposureCurve = new SerializableAnimationCurve(dayNightSystem.cloudLayerAExposureCurve);
                cloudLayerBTintGradient = new SerializableGradient(dayNightSystem.cloudLayerBTintGradient);
                cloudLayerBExposureCurve = new SerializableAnimationCurve(dayNightSystem.cloudLayerBExposureCurve);
            }

            public void Apply(DayNightSystem dayNightSystem) {
                dayNightSystem._editorTimeOfDay = editorTimeOfDay;

                dayNightSystem.horizontalAngle = horizontalAngle;
                dayNightSystem.inclination = inclination;

                dayNightSystem.lightIntensityCurve = lightIntensityCurve.ToAnimationCurve();
                dayNightSystem.lightIntensityMultiplierCurve = lightIntensityMultiplierCurve.ToAnimationCurve();
                dayNightSystem.lightVolumetricMultiplierCurve = lightVolumetricMultiplierCurve.ToAnimationCurve();

                dayNightSystem.lightTemperatureCurve = lightTemperatureCurve.ToAnimationCurve();
                dayNightSystem.colorFilterGradient = colorFilterGradient.ToGradient();
                dayNightSystem.moonSurfaceTintGradient = moonSurfaceTintGradient.ToGradient();
                dayNightSystem.lightTintGradient = lightTintGradient.ToGradient();
                dayNightSystem.enableLightTint = enableLightTint;

                // dayNightSystem.shadowsTintGradient = shadowsTintGradient.ToGradient();

                dayNightSystem.enableLensFlare = enableLensFlare;
                dayNightSystem.lightLensFlare = lightLensFlare.ToAnimationCurve();

                dayNightSystem.exposureCurve = exposureCurve.ToAnimationCurve();
                dayNightSystem.exposureLimitMinCurve = exposureLimitMinCurve.ToAnimationCurve();
                dayNightSystem.exposureLimitMinCurve = exposureLimitMaxCurve.ToAnimationCurve();
                dayNightSystem.exposureHistogramMinCurve = exposureHistogramPercentagesMin.ToAnimationCurve();
                dayNightSystem.exposureHistogramMaxCurve = exposureHistogramPercentagesMax.ToAnimationCurve();

                dayNightSystem.postExposureCurve = postExposureCurve.ToAnimationCurve();
                dayNightSystem.indirectDiffuseCurve = indirectDiffuseCurve.ToAnimationCurve();

                dayNightSystem.fogDensityCurve = fogDensityCurve.ToAnimationCurve();
                dayNightSystem.fogHeightCurve = fogHeightCurve.ToAnimationCurve();
                dayNightSystem.fogAnisotropyCurve = fogAnisotropyCurve.ToAnimationCurve();
                
                dayNightSystem.skyBlendCurve = spaceBlendCurve.ToAnimationCurve();
                dayNightSystem.skyMultiplierCurve = spaceMultiplier.ToAnimationCurve();
                dayNightSystem.skyRotation = spaceRotation;

                dayNightSystem.cloudLayerOpacityCurve = cloudLayerOpacityCurve.ToAnimationCurve();
                dayNightSystem.cloudLayerATintGradient = cloudLayerATintGradient.ToGradient();
                dayNightSystem.cloudLayerAExposureCurve = cloudLayerAExposureCurve.ToAnimationCurve();
                dayNightSystem.cloudLayerBTintGradient = cloudLayerBTintGradient.ToGradient();
                dayNightSystem.cloudLayerBExposureCurve = cloudLayerBExposureCurve.ToAnimationCurve();

                dayNightSystem.EDITOR_SyncTimeParameters();
                dayNightSystem.EDITOR_SyncRotationParameters();
            }

            [System.Serializable]
            public struct SerializableKeyframe {
                public float time;
                public float value;
                public float inTangent;
                public float outTangent;

                public SerializableKeyframe(Keyframe keyframe) {
                    time = keyframe.time;
                    value = keyframe.value;
                    inTangent = keyframe.inTangent;
                    outTangent = keyframe.outTangent;
                }

                public Keyframe ToKeyframe() {
                    return new Keyframe(time, value, inTangent, outTangent);
                }
            }

            [System.Serializable]
            public struct SerializableAnimationCurve {
                public SerializableKeyframe[] keys;
                public WrapMode postWrapMode;
                public WrapMode preWrapMode;

                public SerializableAnimationCurve(AnimationCurve curve) {
                    keys = curve.keys.Select(keyframe => new SerializableKeyframe(keyframe)).ToArray();
                    postWrapMode = curve.postWrapMode;
                    preWrapMode = curve.preWrapMode;
                }

                public AnimationCurve ToAnimationCurve() {
                    AnimationCurve curve = new AnimationCurve();

                    if (keys != null) {
                        curve.keys = keys.Select(serializableKeyframe => serializableKeyframe.ToKeyframe()).ToArray();
                    }

                    curve.postWrapMode = postWrapMode;
                    curve.preWrapMode = preWrapMode;

                    return curve;
                }
            }

            [System.Serializable]
            public struct SerializableGradient {
                public SerializableGradient(Gradient gradient) {
                    alphaKeys = new SerializableGradientAlphaKey[gradient.alphaKeys.Length];
                    for (int i = 0; i < gradient.alphaKeys.Length; i++) {
                        alphaKeys[i] = new SerializableGradientAlphaKey(gradient.alphaKeys[i]);
                    }

                    colorKeys = new SerializableGradientColorKey[gradient.colorKeys.Length];
                    for (int i = 0; i < gradient.colorKeys.Length; i++) {
                        colorKeys[i] = new SerializableGradientColorKey(gradient.colorKeys[i]);
                    }
                }

                public SerializableGradientAlphaKey[] alphaKeys;
                public SerializableGradientColorKey[] colorKeys;

                public Gradient ToGradient() {
                    Gradient gradient = new Gradient {
                        alphaKeys = alphaKeys.Select(key => key.ToGradientAlphaKey()).ToArray(),
                        colorKeys = colorKeys.Select(key => key.ToGradientColorKey()).ToArray()
                    };
                    return gradient;
                }
            }

            [System.Serializable]
            public struct SerializableGradientAlphaKey {
                public float alpha;
                public float time;

                public SerializableGradientAlphaKey(GradientAlphaKey alphaKey) {
                    alpha = alphaKey.alpha;
                    time = alphaKey.time;
                }

                public GradientAlphaKey ToGradientAlphaKey() {
                    return new GradientAlphaKey(alpha, time);
                }
            }

            [System.Serializable]
            public struct SerializableGradientColorKey {
                public Color color;
                public float time;

                public SerializableGradientColorKey(GradientColorKey colorKey) {
                    color = colorKey.color;
                    time = colorKey.time;
                }

                public GradientColorKey ToGradientColorKey() {
                    return new GradientColorKey(color, time);
                }
            }
        }
#endif
    }
}