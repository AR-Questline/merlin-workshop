using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/UpScaling")]
    [VFXBinder("AR/UpScaling binder")]
    public class UpScalingBinder : VFXBinderBase {
        public string Property {
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set => property = value;
        }

        [VFXPropertyBinding("System.Boolean"), SerializeField]
        ExposedProperty property = "UpScalingEnabled";

        public override bool IsValid(VisualEffect component) {
            return component.HasBool(property);
        }

        public override void UpdateBinding(VisualEffect component) {
            var upScaling = World.Any<UpScaling>();
            bool isUpScaling = false;
            if (upScaling != null) {
                isUpScaling = upScaling.IsUpScalingEnabled && upScaling.SliderValue < 1f;
            }

            component.SetBool(property, isUpScaling);
        }

        public override string ToString() {
            return "UpScaling enabled";
        }
    }
}
