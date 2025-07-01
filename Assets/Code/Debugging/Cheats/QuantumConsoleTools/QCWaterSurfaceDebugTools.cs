using System;
using System.Collections.Generic;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using QFSW.QC;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCWaterSurfaceDebugTools {
        static readonly Dictionary<string, Func<WaterSurface, string, bool>> PropertySetters = new() {
            { nameof(WaterSurface.enabled), (ws, value) => {
                bool state = bool.TryParse(value, out var enabled);
                if (state) ws.enabled = enabled;
                return state;
            } },
            { nameof(WaterSurface.timeMultiplier), (ws, value) => float.TryParse(value, out ws.timeMultiplier) },
            { nameof(WaterSurface.scriptInteractions), (ws, value) => bool.TryParse(value, out ws.scriptInteractions) },
            { nameof(WaterSurface.cpuEvaluateRipples), (ws, value) => bool.TryParse(value, out ws.cpuEvaluateRipples) },
            { nameof(WaterSurface.startSmoothness), (ws, value) => float.TryParse(value, out ws.startSmoothness) },
            { nameof(WaterSurface.endSmoothness), (ws, value) => float.TryParse(value, out ws.endSmoothness) },
            { nameof(WaterSurface.smoothnessFadeDistance), (ws, value) => float.TryParse(value, out ws.smoothnessFadeDistance) },
            { nameof(WaterSurface.smoothnessFadeStart), (ws, value) => float.TryParse(value, out ws.smoothnessFadeStart) },
            { nameof(WaterSurface.tessellation), (ws, value) => bool.TryParse(value, out ws.tessellation) },
            { nameof(WaterSurface.maxTessellationFactor), (ws, value) => float.TryParse(value, out ws.maxTessellationFactor) },
            { nameof(WaterSurface.tessellationFactorFadeStart), (ws, value) => float.TryParse(value, out ws.tessellationFactorFadeStart) },
            { nameof(WaterSurface.tessellationFactorFadeRange), (ws, value) => float.TryParse(value, out ws.tessellationFactorFadeRange) },
            { nameof(WaterSurface.refractionColor), (ws, value) => TryParseColor(value, out ws.refractionColor) },
            { nameof(WaterSurface.maxRefractionDistance), (ws, value) => float.TryParse(value, out ws.maxRefractionDistance) },
            { nameof(WaterSurface.absorptionDistance), (ws, value) => float.TryParse(value, out ws.absorptionDistance) },
            { nameof(WaterSurface.scatteringColor), (ws, value) => TryParseColor(value, out ws.scatteringColor) },
            { nameof(WaterSurface.ambientScattering), (ws, value) => float.TryParse(value, out ws.ambientScattering) },
            { nameof(WaterSurface.heightScattering), (ws, value) => float.TryParse(value, out ws.heightScattering) },
            { nameof(WaterSurface.displacementScattering), (ws, value) => float.TryParse(value, out ws.displacementScattering) },
            { nameof(WaterSurface.directLightTipScattering), (ws, value) => float.TryParse(value, out ws.directLightTipScattering) },
            { nameof(WaterSurface.directLightBodyScattering), (ws, value) => float.TryParse(value, out ws.directLightBodyScattering) },
            { nameof(WaterSurface.maximumHeightOverride), (ws, value) => float.TryParse(value, out ws.maximumHeightOverride) },
            { nameof(WaterSurface.caustics), (ws, value) => bool.TryParse(value, out ws.caustics) },
            { nameof(WaterSurface.causticsIntensity), (ws, value) => float.TryParse(value, out ws.causticsIntensity) },
            { nameof(WaterSurface.causticsPlaneBlendDistance), (ws, value) => float.TryParse(value, out ws.causticsPlaneBlendDistance) },
            { nameof(WaterSurface.causticsResolution), (ws, value) => Enum.TryParse(value, out ws.causticsResolution) },
            { nameof(WaterSurface.causticsBand), (ws, value) => int.TryParse(value, out ws.causticsBand) },
            { nameof(WaterSurface.virtualPlaneDistance), (ws, value) => float.TryParse(value, out ws.virtualPlaneDistance) },
            { nameof(WaterSurface.causticsTilingFactor), (ws, value) => float.TryParse(value, out ws.causticsTilingFactor) },
            { nameof(WaterSurface.causticsDirectionalShadow), (ws, value) => bool.TryParse(value, out ws.causticsDirectionalShadow) },
            { nameof(WaterSurface.causticsDirectionalShadowDimmer), (ws, value) => float.TryParse(value, out ws.causticsDirectionalShadowDimmer) },
            { nameof(WaterSurface.renderingLayerMask), (ws, value) => Enum.TryParse(value, out ws.renderingLayerMask) },
            { nameof(WaterSurface.debugMode), (ws, value) => Enum.TryParse(value, out ws.debugMode) },
            { nameof(WaterSurface.waterMaskDebugMode), (ws, value) => Enum.TryParse(value, out ws.waterMaskDebugMode) },
            { nameof(WaterSurface.waterCurrentDebugMode), (ws, value) => Enum.TryParse(value, out ws.waterCurrentDebugMode) },
            { nameof(WaterSurface.currentDebugMultiplier), (ws, value) => float.TryParse(value, out ws.currentDebugMultiplier) },
            { nameof(WaterSurface.waterFoamDebugMode), (ws, value) => Enum.TryParse(value, out ws.waterFoamDebugMode) },
            { nameof(WaterSurface.underWater), (ws, value) => bool.TryParse(value, out ws.underWater) },
            { nameof(WaterSurface.volumeDepth), (ws, value) => float.TryParse(value, out ws.volumeDepth) },
            { nameof(WaterSurface.volumeHeight), (ws, value) => float.TryParse(value, out ws.volumeHeight) },
            { nameof(WaterSurface.volumePrority), (ws, value) => int.TryParse(value, out ws.volumePrority) },
            { nameof(WaterSurface.absorptionDistanceMultiplier), (ws, value) => float.TryParse(value, out ws.absorptionDistanceMultiplier) },
            { nameof(WaterSurface.underWaterAmbientProbeContribution), (ws, value) => float.TryParse(value, out ws.underWaterAmbientProbeContribution) },
            { nameof(WaterSurface.underWaterScatteringColor), (ws, value) => TryParseColor(value, out ws.underWaterScatteringColor) },
            { nameof(WaterSurface.underWaterRefraction), (ws, value) => bool.TryParse(value, out ws.underWaterRefraction) },
            { nameof(WaterSurface.decalRegionSize), (ws, value) => TryParseVector2(value, out ws.decalRegionSize) },
            { nameof(WaterSurface.supportLargeCurrent), (ws, value) => bool.TryParse(value, out ws.supportLargeCurrent) },
            { nameof(WaterSurface.largeCurrentSpeedValue), (ws, value) => float.TryParse(value, out ws.largeCurrentSpeedValue) },
            { nameof(WaterSurface.largeCurrentRegionExtent), (ws, value) => TryParseVector2(value, out ws.largeCurrentRegionExtent) },
            { nameof(WaterSurface.largeCurrentRegionOffset), (ws, value) => TryParseVector2(value, out ws.largeCurrentRegionOffset) },
            { nameof(WaterSurface.largeCurrentMapInfluence), (ws, value) => float.TryParse(value, out ws.largeCurrentMapInfluence) },
            { nameof(WaterSurface.supportRipplesCurrent), (ws, value) => bool.TryParse(value, out ws.supportRipplesCurrent) },
            { nameof(WaterSurface.ripplesCurrentSpeedValue), (ws, value) => float.TryParse(value, out ws.ripplesCurrentSpeedValue) },
            { nameof(WaterSurface.ripplesCurrentRegionExtent), (ws, value) => TryParseVector2(value, out ws.ripplesCurrentRegionExtent) },
            { nameof(WaterSurface.ripplesCurrentRegionOffset), (ws, value) => TryParseVector2(value, out ws.ripplesCurrentRegionOffset) },
            { nameof(WaterSurface.ripplesCurrentMapInfluence), (ws, value) => float.TryParse(value, out ws.ripplesCurrentMapInfluence) },
            { nameof(WaterSurface.deformation), (ws, value) => bool.TryParse(value, out ws.deformation) },
            { nameof(WaterSurface.deformationRes), (ws, value) => Enum.TryParse(value, out ws.deformationRes) },
            { nameof(WaterSurface.foam), (ws, value) => bool.TryParse(value, out ws.foam) },
            { nameof(WaterSurface.foamResolution), (ws, value) => Enum.TryParse(value, out ws.foamResolution) },
            { nameof(WaterSurface.foamPersistenceMultiplier), (ws, value) => float.TryParse(value, out ws.foamPersistenceMultiplier) },
            { nameof(WaterSurface.foamCurrentInfluence), (ws, value) => float.TryParse(value, out ws.foamCurrentInfluence) },
            { nameof(WaterSurface.foamColor), (ws, value) => TryParseColor(value, out ws.foamColor) },
            { nameof(WaterSurface.foamTextureTiling), (ws, value) => float.TryParse(value, out ws.foamTextureTiling) },
            { nameof(WaterSurface.foamSmoothness), (ws, value) => float.TryParse(value, out ws.foamSmoothness) },
            { nameof(WaterSurface.simulationFoamAmount), (ws, value) => float.TryParse(value, out ws.simulationFoamAmount) },
            { nameof(WaterSurface.supportSimulationFoamMask), (ws, value) => bool.TryParse(value, out ws.supportSimulationFoamMask) },
            { nameof(WaterSurface.simulationFoamMaskExtent), (ws, value) => TryParseVector2(value, out ws.simulationFoamMaskExtent) },
            { nameof(WaterSurface.simulationFoamMaskOffset), (ws, value) => TryParseVector2(value, out ws.simulationFoamMaskOffset) },
            { nameof(WaterSurface.repetitionSize), (ws, value) => float.TryParse(value, out ws.repetitionSize) },
            { nameof(WaterSurface.largeOrientationValue), (ws, value) => float.TryParse(value, out ws.largeOrientationValue) },
            { nameof(WaterSurface.largeWindSpeed), (ws, value) => float.TryParse(value, out ws.largeWindSpeed) },
            { nameof(WaterSurface.largeChaos), (ws, value) => float.TryParse(value, out ws.largeChaos) },
            { nameof(WaterSurface.largeBand0Multiplier), (ws, value) => float.TryParse(value, out ws.largeBand0Multiplier) },
            { nameof(WaterSurface.largeBand0FadeMode), (ws, value) => Enum.TryParse(value, out ws.largeBand0FadeMode) },
            { nameof(WaterSurface.largeBand0FadeStart), (ws, value) => float.TryParse(value, out ws.largeBand0FadeStart) },
            { nameof(WaterSurface.largeBand0FadeDistance), (ws, value) => float.TryParse(value, out ws.largeBand0FadeDistance) },
            { nameof(WaterSurface.largeBand1Multiplier), (ws, value) => float.TryParse(value, out ws.largeBand1Multiplier) },
            { nameof(WaterSurface.largeBand1FadeMode), (ws, value) => Enum.TryParse(value, out ws.largeBand1FadeMode) },
            { nameof(WaterSurface.largeBand1FadeStart), (ws, value) => float.TryParse(value, out ws.largeBand1FadeStart) },
            { nameof(WaterSurface.largeBand1FadeDistance), (ws, value) => float.TryParse(value, out ws.largeBand1FadeDistance) },
            { nameof(WaterSurface.ripples), (ws, value) => bool.TryParse(value, out ws.ripples) },
            { nameof(WaterSurface.ripplesMotionMode), (ws, value) => Enum.TryParse(value, out ws.ripplesMotionMode) },
            { nameof(WaterSurface.ripplesOrientationValue), (ws, value) => float.TryParse(value, out ws.ripplesOrientationValue) },
            { nameof(WaterSurface.ripplesWindSpeed), (ws, value) => float.TryParse(value, out ws.ripplesWindSpeed) },
            { nameof(WaterSurface.ripplesChaos), (ws, value) => float.TryParse(value, out ws.ripplesChaos) },
            { nameof(WaterSurface.ripplesFadeMode), (ws, value) => Enum.TryParse(value, out ws.ripplesFadeMode) },
            { nameof(WaterSurface.ripplesFadeStart), (ws, value) => float.TryParse(value, out ws.ripplesFadeStart) },
            { nameof(WaterSurface.ripplesFadeDistance), (ws, value) => float.TryParse(value, out ws.ripplesFadeDistance) },
            { nameof(WaterSurface.simulationMask), (ws, value) => bool.TryParse(value, out ws.simulationMask) },
            { nameof(WaterSurface.maskRes), (ws, value) => Enum.TryParse(value, out ws.maskRes) },
            { nameof(WaterSurface.waterMaskRemap), (ws, value) => TryParseVector2(value, out ws.waterMaskRemap) },
            { nameof(WaterSurface.waterMaskExtent), (ws, value) => TryParseVector2(value, out ws.waterMaskExtent) },
            { nameof(WaterSurface.waterMaskOffset), (ws, value) => TryParseVector2(value, out ws.waterMaskOffset) },
        };

        public static IEnumerable<string> PropertyNames => PropertySetters.Keys;


        static string WaterSurfaceToEditName { get; set; } = "";


        [Command("set-water", "Set water surface to edit")] [UnityEngine.Scripting.Preserve]
        static void SetWaterSurfaceToEdit([WaterSurfaceName] string waterSurfaceName) {
            WaterSurfaceToEditName = waterSurfaceName;
        }
        
        [Command("change-water", "Changes water surface properties")] [UnityEngine.Scripting.Preserve]
        static void ChangeWaterSurface([WaterSurfacePropertyName] string propertyName, string value) {
            var waterSurface = FindWaterSurface(WaterSurfaceToEditName);
            
            if (waterSurface == null) {
                Log.Important?.Error($"WaterSurface with game object name {WaterSurfaceToEditName} not found");
                return;
            }

            if (!PropertySetters.TryGetValue(propertyName, out var propertySetterFunc)) {
                Log.Important?.Error($"Property {propertyName} is invalid");
                return;
            }
            
            if (!propertySetterFunc(waterSurface, value)) {
                Log.Important?.Error($"Failed to set property {propertyName} to {value}");
            }
        }

        static WaterSurface FindWaterSurface(string name) {
            var gameObject = GameObject.Find(name);
            return gameObject != null ? gameObject.GetComponentInChildren<WaterSurface>() : null;
        }
        
        static bool TryParseVector2(string value, out Vector2 vector) {
            var parts = value.Split(';');
            
            if (parts.Length == 2 
                && float.TryParse(parts[0], out var x) 
                && float.TryParse(parts[1], out var y)) 
            {
                vector = new Vector2(x, y);
                return true;
            }
            
            vector = default;
            return false;
        }

        static bool TryParseColor(string value, out Color color) {
            var parts = value.Split(';');
            
            if (parts.Length == 4
                && float.TryParse(parts[0], out var r)
                && float.TryParse(parts[1], out var g)
                && float.TryParse(parts[2], out var b)
                && float.TryParse(parts[3], out var a))
            {
                color = new Color(r, g, b, a);
                return true;
            }
            
            color = default;
            return false;
        }
    }
}