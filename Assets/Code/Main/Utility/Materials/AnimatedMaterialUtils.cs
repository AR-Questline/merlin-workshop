using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics;
using Awaken.Utility.Collections;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Materials {
    public static class AnimatedMaterialUtils {
        [UnityEngine.Scripting.Preserve]
        public static Material[] GetAnimatedMaterials(GameObject gameObject, bool withDisabled = false) {
            var materials = gameObject.GetComponentsInChildren<Renderer>(withDisabled)
                .Where(r => !(r is ParticleSystemRenderer))
                .Where(r => r.gameObject.GetComponentInParent<IgnoreOnMaterialChange>() == null)
                .SelectMany(r => r.materials)
                .ToArray();

            return materials;
        }

        [UnityEngine.Scripting.Preserve]
        public static MultiMap<GameObject, Material> GetAnimatedGameObjectsWithItsMaterials(GameObject gameObject) {
            MultiMap<GameObject, Material> gameObjectsWithMaterials = new MultiMap<GameObject, Material>();
            foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>()) {
                if (renderer is ParticleSystemRenderer || renderer.gameObject.GetComponentInParent<IgnoreOnMaterialChange>() != null) {
                    continue;
                }
                foreach (var material in renderer.materials) {
                    gameObjectsWithMaterials.Add(renderer.gameObject, material);
                }
            }
            return gameObjectsWithMaterials;
        }

        [UnityEngine.Scripting.Preserve]
        public static Sequence ChangeMaterialState(StateSettings stateToApply, Material[] materialsToChange) {
            var mergeSequence = DOTween.Sequence();
            var duration = stateToApply.duration;
            float factor = Mathf.Pow(2, stateToApply.auraColorIntensity);
            Color hdrAuraColor = new Color(stateToApply.auraColor.r * factor, stateToApply.auraColor.g * factor, stateToApply.auraColor.b * factor, stateToApply.auraColor.a);
            foreach (Material material in materialsToChange) {
                var sequence = DOTween.Sequence()
                    .Join(material.DOColor(hdrAuraColor, AuraColorID, duration))
                    .Join(material.DOFloat(stateToApply.auraFresnelBias, AuraFresnelBiasID, duration))
                    .Join(material.DOFloat(stateToApply.auraFresnelPower, AuraFresnelPowerID, duration))
                    .Join(material.DOFloat(stateToApply.auraFresnelScale, AuraFresnelScaleID, duration))
                    .Join(material.DOFloat(stateToApply.auraFlashingStrength, AuraFlashingStrengthID, duration))
                    .Join(material.DOFloat(stateToApply.auraFlashingSpeed, AuraFlashingSpeedID, duration))
                    .Join(material.DOFloat(stateToApply.auraFlashingClamp, AuraFlashingClampID, duration))
                    .Join(material.DOFloat(stateToApply.auraIntensity, AuraIntensityID, duration))
                    .Join(material.DOFloat(stateToApply.auraPower, AuraPowerID, duration));
                if (material.HasProperty(DesaturateID)) {
                    sequence.Join(material.DOFloat(stateToApply.desaturate, DesaturateID, duration));
                }

                if (material.HasProperty(DarkenID)) {
                    sequence.Join(material.DOFloat(stateToApply.darken, DarkenID, duration));
                }

                mergeSequence.Join(sequence);
            }

            return mergeSequence;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static Sequence ChangeMaterialDarkenState(StateSettings stateToApply, Material[] materialsToChange) {
            var mergeSequence = DOTween.Sequence();
            var duration = stateToApply.duration;
            foreach (Material material in materialsToChange) {
                var sequence = DOTween.Sequence();
                if (material.HasProperty(DesaturateID)) {
                    sequence.Join(material.DOFloat(stateToApply.desaturate, DesaturateID, duration));
                }

                if (material.HasProperty(DarkenID)) {
                    sequence.Join(material.DOFloat(stateToApply.darken, DarkenID, duration));
                }

                mergeSequence.Join(sequence);
            }

            return mergeSequence;
        }

        [UnityEngine.Scripting.Preserve]
        public static void ChangeOutline(OutlineSettings settings, Material material) {
            material.SetColor(OutlineColorID, settings.color);
            material.SetFloat(OutlineWidthID, settings.width);
            material.SetFloat(OutlineEmissiveIntensity, settings.intensity);
        }

        // === Data
        [UnityEngine.Scripting.Preserve]
        public static readonly StateSettings Normal = new StateSettings() {
            auraPower = 1,
            duration = 0.5f,
        };

        [UnityEngine.Scripting.Preserve]
        public static readonly StateSettings Vulnerable = new StateSettings() {
            auraIntensity = 0.25f,
            auraColor = new Color(0.75f, 0.05f, 0.0f),
            auraColorIntensity = 2f,
            auraFresnelBias = 0.2f,
            auraFresnelPower = 2,
            auraFresnelScale = 1.5f,
            auraFlashingStrength = 1,
            auraFlashingSpeed = 2f,
            auraFlashingClamp = 0.75f,
            auraPower = 4,
            duration = 0.5f,
        };

        [UnityEngine.Scripting.Preserve]
        public static readonly StateSettings Stunned = new StateSettings() {
            auraIntensity = 0.25f,
            auraColor = new Color(0.7450981f, 1.082353f, 1.498039f),
            auraColorIntensity = 2f,
            auraFresnelBias = 0.25f,
            auraFresnelPower = 2.0f,
            auraFresnelScale = 2.0f,
            auraPower = 1,
            duration = 0.5f,
        };

        [UnityEngine.Scripting.Preserve]
        public static readonly StateSettings Disabled = new StateSettings() {
            auraIntensity = 0f,
            desaturate = 1.0f,
            darken = 0.25f,
            duration = 0.05f,
        };

        [UnityEngine.Scripting.Preserve]
        public static readonly StateSettings ClassSelectionDisabled = new StateSettings() {
            auraIntensity = 0f,
            desaturate = 1.0f,
            darken = 1f,
            duration = 0
        };
        
        static readonly int AuraColorID = Shader.PropertyToID("_AuraColor");
        static readonly int AuraFresnelBiasID = Shader.PropertyToID("_AuraFresnelBias");
        static readonly int AuraFresnelPowerID = Shader.PropertyToID("_AuraFresnelPower");
        static readonly int AuraFresnelScaleID = Shader.PropertyToID("_AuraFresnelScale");
        static readonly int AuraFlashingStrengthID = Shader.PropertyToID("_AuraFlashingStrength");
        static readonly int AuraFlashingSpeedID = Shader.PropertyToID("_AuraFlashingSpeed");
        static readonly int AuraFlashingClampID = Shader.PropertyToID("_AuraFlashingClamp");
        static readonly int AuraIntensityID = Shader.PropertyToID("_AuraIntensity");
        static readonly int AuraPowerID = Shader.PropertyToID("_AuraPower");
        static readonly int DesaturateID = Shader.PropertyToID("_Desaturate");
        static readonly int DarkenID = Shader.PropertyToID("_Darken");

        [UnityEngine.Scripting.Preserve]
        public static readonly OutlineSettings TargetOutline = new OutlineSettings(){
            color = new Color(0.75f, 0.6f, 0.0f),
            width = 0.12f,
            intensity = 8
        };
        
        [UnityEngine.Scripting.Preserve]
        public static readonly OutlineSettings PossibleTargetOutline = new OutlineSettings(){
            color = new Color(0.8f, 0.7f, 0.3f),
            width = 0.08f,
            intensity = 5
        };

        static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
        static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");
        static readonly int OutlineEmissiveIntensity = Shader.PropertyToID("_OutlineEmissiveIntensity");
    }

    public class StateSettings {
        public Color auraColor;
        public float auraColorIntensity;
        public float auraFresnelBias;
        public float auraFresnelPower;
        public float auraFresnelScale;
        public float auraFlashingStrength;
        public float auraFlashingSpeed;
        public float auraFlashingClamp;
        public float auraIntensity;
        public float auraPower;
        public float duration;
        public float desaturate;
        public float darken;
    }

    public class OutlineSettings {
        public Color color;
        public float width;
        public float intensity;
    }
}