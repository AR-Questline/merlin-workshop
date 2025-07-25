using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This attribute is used to associate a unique ID to a sky class.
    /// This is needed to be able to automatically register sky classes and avoid collisions and refactoring class names causing data compatibility issues.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SkyUniqueID : Attribute
    {
        internal readonly int uniqueID;

        /// <summary>
        /// Attribute SkyUniqueID constructor.
        /// </summary>
        /// <param name="uniqueID">Sky unique ID. Needs to be different from all other registered unique IDs.</param>
        public SkyUniqueID(int uniqueID)
        {
            this.uniqueID = uniqueID;
        }
    }

    /// <summary>
    /// Environment Update volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class EnvUpdateParameter : VolumeParameter<EnvironmentUpdateMode>
    {
        /// <summary>
        /// Environment Update parameter constructor.
        /// </summary>
        /// <param name="value">Environment Update Mode parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public EnvUpdateParameter(EnvironmentUpdateMode value, bool overrideState = false)
            : base(value, overrideState) { }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + ((int)value).GetHashCode();

                return hash;
            }
        }
    }

    /// <summary>
    /// Sky Intensity Mode.
    /// </summary>
    public enum SkyIntensityMode
    {
        /// <summary>Intensity is expressed as an exposure.</summary>
        Exposure,
        /// <summary>Intensity is expressed in lux.</summary>
        Lux,
        /// <summary>Intensity is expressed as a multiplier.</summary>
        Multiplier,
    }


    /// <summary>
    /// Backplate Type for HDRISKy.
    /// </summary>
    public enum BackplateType
    {
        /// <summary>Shape of backplate is a Disc.</summary>
        Disc,
        /// <summary>Shape of backplate is a Rectangle.</summary>
        Rectangle,
        /// <summary>Shape of backplate is a Ellispe.</summary>
        Ellipse,
        /// <summary>Shape of backplate is a Infinite Plane.</summary>
        Infinite
    }

    /// <summary>
    /// Backplate Type volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class BackplateTypeParameter : VolumeParameter<BackplateType>
    {
        /// <summary>
        /// Backplate Type volume parameter constructor.
        /// </summary>
        /// <param name="value">Backplate Type parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public BackplateTypeParameter(BackplateType value, bool overrideState = false)
            : base(value, overrideState) { }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + ((int)value).GetHashCode();

                return hash;
            }
        }
    }

    /// <summary>
    /// Sky Intensity volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyIntensityParameter : VolumeParameter<SkyIntensityMode>
    {
        /// <summary>
        /// Sky Intensity volume parameter constructor.
        /// </summary>
        /// <param name="value">Sky Intensity parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public SkyIntensityParameter(SkyIntensityMode value, bool overrideState = false)
            : base(value, overrideState) { }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + ((int)value).GetHashCode();

                return hash;
            }
        }
    }

    /// <summary>
    /// Base class for custom Sky Settings.
    /// </summary>
    public abstract class SkySettings : VolumeComponent
    {
        /// <summary>Rotation of the sky.</summary>
        [Tooltip("Sets the rotation of the sky.")]
        public ClampedFloatParameter rotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Intensity mode of the sky.</summary>
        [Tooltip("Specifies the intensity mode HDRP uses for the sky.")]
        public SkyIntensityParameter skyIntensityMode = new SkyIntensityParameter(SkyIntensityMode.Exposure);
        /// <summary>Exposure of the sky.</summary>
        [Tooltip("Sets the exposure of the sky in EV.")]
        public FloatParameter exposure = new FloatParameter(0.0f);
        /// <summary>Intensity Multipler of the sky.</summary>
        [Tooltip("Sets the intensity multiplier for the sky.")]
        public MinFloatParameter multiplier = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Informative helper that displays the relative intensity (in Lux) for the current HDR texture set in HDRI Sky.</summary>
        [Tooltip("Informative helper that displays the relative intensity (in Lux) for the current HDR texture set in HDRI Sky.")]
        public MinFloatParameter upperHemisphereLuxValue = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Informative helper that displays Show the color of Shadow.</summary>
        [Tooltip("Informative helper that displays Show the color of Shadow.")]
        public Vector3Parameter upperHemisphereLuxColor = new Vector3Parameter(new Vector3(0, 0, 0));
        /// <summary>Absolute intensity (in lux) of the sky.</summary>
        [Tooltip("Sets the absolute intensity (in Lux) of the current HDR texture set in HDRI Sky. Functions as a Lux intensity multiplier for the sky.")]
        public FloatParameter desiredLuxValue = new FloatParameter(20000);
        /// <summary>Update mode of the sky.</summary>
        [Tooltip("Specifies when HDRP updates the environment lighting. When set to OnDemand, use HDRenderPipeline.RequestSkyEnvironmentUpdate() to request an update.")]
        public EnvUpdateParameter updateMode = new EnvUpdateParameter(EnvironmentUpdateMode.OnChanged);
        /// <summary>In case of real-time update mode, time between updates. 0 means every frame.</summary>
        [Tooltip("Sets the period, in seconds, at which HDRP updates the environment ligting (0 means HDRP updates it every frame).")]
        public MinFloatParameter updatePeriod = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>True if the sun disk should be included in the baking information (where available).</summary>
        [Tooltip("When enabled, HDRP uses the Sun Disk in baked lighting.")]
        public BoolParameter includeSunInBaking = new BoolParameter(false);


        static Dictionary<Type, int> skyUniqueIDs = new Dictionary<Type, int>();

        /// <summary>
        /// Returns the hash code of the sky parameters.
        /// </summary>
        /// <param name="camera">The camera we want to use to compute the hash of the sky.</param>
        /// <returns>The hash code of the sky parameters.</returns>
        virtual public int GetHashCode(Camera camera)
        {
            // By default we don't need to consider the camera position.
            return GetHashCode();
        }

        /// <summary>
        /// Returns the hash code of the sky parameters. When used with PBR Sky please use the GetHashCode variant that takes a camera as parameter.
        /// </summary>
        /// <returns>The hash code of the sky parameters.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
#if UNITY_2019_3 // In 2019.3, when we call GetHashCode on a VolumeParameter it generate garbage (due to the boxing of the generic parameter)
                // UpdateMode and period should not be part of the hash as they do not influence rendering itself.
                int hash = 13;
                hash = hash * 23 + rotation.value.GetHashCode();
                hash = hash * 23 + exposure.value.GetHashCode();
                hash = hash * 23 + multiplier.value.GetHashCode();
                hash = hash * 23 + desiredLuxValue.value.GetHashCode();
                hash = hash * 23 + skyIntensityMode.value.GetHashCode();
                hash = hash * 23 + includeSunInBaking.value.GetHashCode();

                hash = hash * 23 + rotation.overrideState.GetHashCode();
                hash = hash * 23 + exposure.overrideState.GetHashCode();
                hash = hash * 23 + multiplier.overrideState.GetHashCode();
                hash = hash * 23 + desiredLuxValue.overrideState.GetHashCode();
                hash = hash * 23 + skyIntensityMode.overrideState.GetHashCode();
                hash = hash * 23 + includeSunInBaking.overrideState.GetHashCode();
#else
                // UpdateMode and period should not be part of the hash as they do not influence rendering itself.
                int hash = 13;
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + exposure.GetHashCode();
                hash = hash * 23 + multiplier.GetHashCode();
                hash = hash * 23 + desiredLuxValue.GetHashCode();
                hash = hash * 23 + skyIntensityMode.GetHashCode();
                hash = hash * 23 + includeSunInBaking.GetHashCode();
#endif

                return hash;
            }
        }

        /// <summary>
        /// Returns the sky type unique ID.
        /// Use this to override the skyType in the Visual Environment volume component.
        /// </summary>
        /// <typeparam name="T">Type of the sky.</typeparam>
        /// <returns>The unique ID for the requested sky type.</returns>
        public static int GetUniqueID<T>()
        {
            return GetUniqueID(typeof(T));
        }

        /// <summary>
        /// Returns the sky type unique ID.
        /// Use this to override the skyType in the Visual Environment volume component.
        /// </summary>
        /// <param name="type">Type of the sky.</param>
        /// <returns>The unique ID for the requested sky type.</returns>
        public static int GetUniqueID(Type type)
        {
            int uniqueID;

            if (!skyUniqueIDs.TryGetValue(type, out uniqueID))
            {
                var uniqueIDs = type.GetCustomAttributes(typeof(SkyUniqueID), false);
                uniqueID = (uniqueIDs.Length == 0) ? -1 : ((SkyUniqueID)uniqueIDs[0]).uniqueID;
                skyUniqueIDs[type] = uniqueID;
            }

            return uniqueID;
        }

        /// <summary>
        /// Returns the sky intensity as determined by this SkySetting.
        /// </summary>
        /// <returns>The sky intensity.</returns>
        public float GetIntensityFromSettings()
        {
            float skyIntensity = 1.0f;
            switch (skyIntensityMode.value)
            {
                case SkyIntensityMode.Exposure:
                    // Note: Here we use EV100 of sky as a multiplier, so it is the opposite of when use with a Camera
                    // because for sky/light, higher EV mean brighter, but for camera higher EV mean darker scene
                    skyIntensity *= ColorUtils.ConvertEV100ToExposure(-exposure.value);
                    break;
                case SkyIntensityMode.Multiplier:
                    skyIntensity *= multiplier.value;
                    break;
                case SkyIntensityMode.Lux:
                    skyIntensity *= desiredLuxValue.value / Mathf.Max(upperHemisphereLuxValue.value, 1e-5f);
                    break;
            }
            return skyIntensity;
        }

        /// <summary>
        /// Determines if the SkySettings is significantly divergent from another. This is going to be used to determine whether
        /// to reset completely the ambient probe instead of using previous one when waiting for current data upon changes.
        /// Override this to have a per-sky specific heuristic.
        /// </summary>
        /// <param name="otherSettings">The settings to compare with.</param>
        /// <returns>Whether the settings are deemed very different.</returns>
        public virtual bool SignificantlyDivergesFrom(SkySettings otherSettings)
        {
            if (otherSettings == null || otherSettings.GetSkyRendererType() != GetSkyRendererType())
                return true;

            float thisIntensity = GetIntensityFromSettings();
            float otherIntensity = otherSettings.GetIntensityFromSettings();

            // This is an arbitrary difference threshold. This needs to be re-evaluated in case it is proven problematic
            float intensityRatio = thisIntensity > otherIntensity ? (thisIntensity / otherIntensity) : (otherIntensity / thisIntensity);
            const float ratioThreshold = 3.0f;
            return intensityRatio > ratioThreshold;
        }

        /// <summary>
        /// Returns the class type of the SkyRenderer associated with this Sky Settings.
        /// </summary>
        /// <returns>The class type of the SkyRenderer associated with this Sky Settings.</returns>
        public abstract Type GetSkyRendererType();

        // Keeping this API internal for now.
        // It's required for PBR sky to interact correctly with baking but we are not 100% set on the interface yet.
        internal virtual Vector3 EvaluateAtmosphericAttenuation(Vector3 sunDirection, Vector3 cameraPosition)
        {
            return Vector3.one;
        }
    }
}
