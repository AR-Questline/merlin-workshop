//#define PROPERTY_BY_QUALITY_EXTRA_DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Settings.Controllers {
    public class VfxPropertyByQualityController : StartDependentView<VfxQuality> {
        [SerializeField]
        PerVisualEffectPropertyQualities[] visualEffectProperties = Array.Empty<PerVisualEffectPropertyQualities>();

        protected override void OnInitialize() {
            Target.ListenTo(Setting.Events.SettingRefresh, SettingChanged, this);
            SettingChanged(Target);
        }

        void SettingChanged(Setting setting) {
            var vfxSettings = (VfxQuality)setting;
            var qualityIndex = vfxSettings.ActiveIndex;
            foreach (var perVisualEffectPropertyQuality in visualEffectProperties) {
                perVisualEffectPropertyQuality.ApplyNewQuality(qualityIndex);
            }
        }

#if UNITY_EDITOR
        [ButtonGroup("Quality", Order = -1), Button("Low"), ShowIf(nameof(IsPlaying)), GUIColor(nameof(IsLowColor))]
        void SwitchToLow() {
            Target.Option.OptionInt = 0;
            Target.Apply(out _);
        }
        Color IsLowColor() {
            if (Target == null)
                return Color.white;
            return Target.ActiveIndex == 0 ? Color.green : Color.white;
        }

        [ButtonGroup("Quality"), Button("Medium"), ShowIf(nameof(IsPlaying)), GUIColor(nameof(IsMediumColor))]
        void SwitchToMedium() {
            Target.Option.OptionInt = 1;
            Target.Apply(out _);
        }
        Color IsMediumColor() {
            if (Target == null)
                return Color.white;
            return Target.ActiveIndex == 1 ? Color.green : Color.white;
        }

        [ButtonGroup("Quality"), Button("High"), ShowIf(nameof(IsPlaying)), GUIColor(nameof(IsHighColor))]
        void SwitchToHigh() {
            Target.Option.OptionInt = 2;
            Target.Apply(out _);
        }
        Color IsHighColor() {
            if (Target == null)
                return Color.white;
            return Target.ActiveIndex == 2 ? Color.green : Color.white;
        }

        bool IsPlaying() {
            return Application.isPlaying;
        }
#endif

        [Serializable]
        public class PerVisualEffectPropertyQualities {
#if UNITY_EDITOR
            static readonly Type[] AvailableTypes = typeof(VfxPropertyByQualityController).GetNestedTypes()
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(VfxPropertyQuality)))
                .ToArray();
#endif

            [OnValueChanged(nameof(RevalidateProperties)), PropertyOrder(0)]
            public VisualEffect visualEffect;
            [SerializeReference, ShowIf(nameof(CanAdd)), PropertyOrder(2), HideReferenceObjectPicker, ListDrawerSettings(HideAddButton = true)]
            public VfxPropertyQuality[] properties = Array.Empty<VfxPropertyQuality>();

            [ValueDropdown(nameof(PossibleProperties)), ShowInInspector, PropertyOrder(1)]
            public string AddProperty {
                get => string.Empty;
                set => OnAddProperty(value);
            }

            public void ApplyNewQuality(int quality) {
                if (visualEffect == null) {
                    return;
                }
                foreach (var vfxPropertyQuality in properties) {
                    vfxPropertyQuality.ApplyNewQuality(visualEffect, quality);
                }
            }

            // === Editor
            bool CanAdd() {
                return visualEffect != null;
            }

            void RevalidateProperties() {
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                foreach (var vfxPropertyQuality in properties) {
                    vfxPropertyQuality.EDITOR_visualEffect = visualEffect;
                }
#endif
            }

            string[] PossibleProperties() {
                if (visualEffect == null) {
                    return Array.Empty<string>();
                }
                var allProperties = new List<VFXExposedProperty>(24);
                visualEffect.visualEffectAsset.GetExposedProperties(allProperties);
                var propertyNames = new List<string>(allProperties.Count);
                foreach (var vfxExposedProperty in allProperties) {
                    var alreadyUsed = false;
                    foreach (var vfxPropertyQuality in properties) {
                        if ((string)vfxPropertyQuality.property == vfxExposedProperty.name) {
                            alreadyUsed = true;
                            break;
                        }
                    }
                    if (!alreadyUsed) {
                        propertyNames.Add(vfxExposedProperty.name);
                    }
                }
                return propertyNames.ToArray();
            }

            void OnAddProperty(string propertyName) {
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                if (string.IsNullOrWhiteSpace(propertyName)) {
                    return;
                }

                var allProperties = new List<VFXExposedProperty>(24);
                visualEffect.visualEffectAsset.GetExposedProperties(allProperties);
                foreach (var vfxExposedProperty in allProperties) {
                    if (vfxExposedProperty.name == propertyName) {
                        var type = vfxExposedProperty.type;
                        foreach (var availableType in AvailableTypes) {
                            if (availableType.BaseType.GetGenericArguments()[1] == type) {
                                var newProperty = (VfxPropertyQuality)Activator.CreateInstance(availableType);
                                newProperty.EDITOR_visualEffect = visualEffect;
                                newProperty.property = propertyName;
                                newProperty.InitDefaults(visualEffect);
                                Array.Resize(ref properties, properties.Length + 1);
                                properties[^1] = newProperty;
                                return;
                            }
                        }
                        return;
                    }
                }
#endif
            }
        }

        [Serializable]
        public abstract class VfxPropertyQuality {
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
            [HideInInspector] public VisualEffect EDITOR_visualEffect;
#endif
            [HideInEditorMode] public VfxProperty property;
            [ValueDropdown(nameof(PossibleProperties)), ShowInInspector, PropertyOrder(-1)] public string PropertyName {
                get => (string)property;
                set => property = value;
            }

            public abstract void ApplyNewQuality(VisualEffect effect, int quality);
            public abstract void InitDefaults(VisualEffect effect);

            string[] PossibleProperties() {
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                if (EDITOR_visualEffect == null) {
                    return Array.Empty<string>();
                }
                var allProperties = new List<VFXExposedProperty>(24);
                EDITOR_visualEffect.visualEffectAsset.GetExposedProperties(allProperties);
                var propertyNames = new List<string>(allProperties.Count);
                foreach (var vfxExposedProperty in allProperties) {
                    if (IsValidProperty(vfxExposedProperty)) {
                        propertyNames.Add(vfxExposedProperty.name);
                    }
                }
                return propertyNames.ToArray();
#else
                return Array.Empty<string>();
#endif
            }

            protected abstract bool IsValidProperty(in VFXExposedProperty vfxExposedProperty);
            
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
            protected static void EXTRALOGGING_LogMissingProperty(VisualEffect effect, VfxProperty property) {
                Log.Important?.Warning("Missing property: " + property.ToString() + " on: " + effect.gameObject.HierarchyPath());
            }
#endif
        }

        [Serializable]
        public abstract class VfxPropertyQuality<T, U> : VfxPropertyQuality {
            public T low;
            public T medium;
            public T high;

            public sealed override void ApplyNewQuality(VisualEffect effect, int quality) {
                var value = quality switch {
                    0 => low,
                    1 => medium,
                    2 => high,
                    _ => low
                };
                ApplyProperty(effect, value);
            }

            protected abstract void ApplyProperty(VisualEffect effect, T value);

            protected sealed override bool IsValidProperty(in VFXExposedProperty vfxExposedProperty) {
                return vfxExposedProperty.type == typeof(U);
            }
        }

        [Serializable]
        public sealed class FloatVfxPropertyQuality : VfxPropertyQuality<float, float> {
            protected override void ApplyProperty(VisualEffect effect, float value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasFloat(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetFloat(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetFloat(property);
                medium = high * 0.7f;
                low = high * 0.45f;
            }
        }

        [Serializable]
        public sealed class IntVfxPropertyQuality : VfxPropertyQuality<int, int> {
            protected override void ApplyProperty(VisualEffect effect, int value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasInt(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetInt(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetInt(property);
                medium = (int)(high * 0.7f);
                low = (int)(high * 0.45f);
            }
        }

        [Serializable]
        public sealed class UIntVfxPropertyQuality : VfxPropertyQuality<uint, uint> {
            protected override void ApplyProperty(VisualEffect effect, uint value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasUInt(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetUInt(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetUInt(property);
                medium = (uint)(high * 0.7f);
                low = (uint)(high * 0.45f);
            }
        }

        [Serializable]
        public sealed class BoolVfxPropertyQuality : VfxPropertyQuality<bool, bool> {
            protected override void ApplyProperty(VisualEffect effect, bool value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasBool(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetBool(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetBool(property);
                medium = high;
                low = !high;
            }
        }

        [Serializable]
        public sealed class ColorVfxPropertyQuality : VfxPropertyQuality<Color, Vector4> {
            protected override void ApplyProperty(VisualEffect effect, Color value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasVector4(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetVector4(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetVector4(property);
                medium = high;
                low = high;
            }
        }

        [Serializable]
        public sealed class Float4VfxPropertyQuality : VfxPropertyQuality<float4, Vector4> {
            protected override void ApplyProperty(VisualEffect effect, float4 value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasVector4(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetVector4(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetVector4(property);
                medium = high;
                low = high;
            }
        }

        [Serializable]
        public sealed class Float3VfxPropertyQuality : VfxPropertyQuality<float3, Vector3> {
            protected override void ApplyProperty(VisualEffect effect, float3 value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasVector3(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetVector3(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetVector3(property);
                medium = high;
                low = high;
            }
        }

        [Serializable]
        public sealed class Float2VfxPropertyQuality : VfxPropertyQuality<float2, Vector2> {
            protected override void ApplyProperty(VisualEffect effect, float2 value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasVector2(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetVector2(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetVector2(property);
                medium = high;
                low = high;
            }
        }

        [Serializable]
        public sealed class AnimationCurveVfxPropertyQuality : VfxPropertyQuality<AnimationCurve, AnimationCurve> {
            protected override void ApplyProperty(VisualEffect effect, AnimationCurve value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasAnimationCurve(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetAnimationCurve(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetAnimationCurve(property);
                medium = new AnimationCurve(high.keys);
                low = new AnimationCurve(high.keys);
            }
        }

        [Serializable]
        public sealed class GradientVfxPropertyQuality : VfxPropertyQuality<Gradient, Gradient> {
            protected override void ApplyProperty(VisualEffect effect, Gradient value) {
#if PROPERTY_BY_QUALITY_EXTRA_DEBUG
                if (!effect.HasGradient(property)) {
                    EXTRALOGGING_LogMissingProperty(effect, property);
                }
#endif
                if (property.ToString() == "") {
                    Log.Debug?.Warning($"{effect.name} property name is not set", effect);
                    return;
                }
                effect.SetGradient(property, value);
            }

            public override void InitDefaults(VisualEffect effect) {
                high = effect.GetGradient(property);
                medium = new Gradient();
                medium.SetKeys(high.colorKeys, high.alphaKeys);
                low = new Gradient();
                low.SetKeys(high.colorKeys, high.alphaKeys);
            }
        }
    }
}