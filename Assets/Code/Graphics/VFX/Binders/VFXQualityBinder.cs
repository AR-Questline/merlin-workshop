using System;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Quality")]
    [VFXBinder("AR/Quality binder")]
    public class VFXQualityBinder : VFXBinderBase {
        public string Property {
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set {
                property = value;
                UpdateSubProperties();
            }
        }

        [VFXPropertyBinding("Awaken.TG.Graphics.VFX.VFXQuality"), SerializeField]
        VfxProperty property = "VfxQuality";
        VfxProperty _qualityIndex;

        protected override void OnEnable() {
            base.OnEnable();
            UpdateSubProperties();
        }

        void OnValidate() {
            UpdateSubProperties();
        }

        public override bool IsValid(VisualEffect component) {
            return component.HasUInt(_qualityIndex);
        }

        public override void UpdateBinding(VisualEffect component) {
            component.SetUInt(_qualityIndex, GetQualityIndex());
        }

        public override string ToString() {
            var quality = GetQualityIndex();
            return "Vfx Quality: " + HumanName(quality);
        }

        uint GetQualityIndex() {
            var vfxQuality = World.Any<VfxQuality>();
            return (uint)(vfxQuality?.ActiveIndex ?? 0);
        }

        void UpdateSubProperties() {
            var mainProperty = property.ToString();
            _qualityIndex = mainProperty + "_qualityIndex";
        }

        string HumanName(uint index) {
            return index switch {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                _ => "Unknown, check with programmer",
            };
        }
    }

    [VFXType(VFXTypeAttribute.Usage.Default | VFXTypeAttribute.Usage.GraphicsBuffer), Serializable]
    public struct VFXQuality {
        [UnityEngine.Scripting.Preserve] public uint qualityIndex;

        public VFXQuality(uint qualityIndex) {
            this.qualityIndex = qualityIndex;
        }
    }
}
