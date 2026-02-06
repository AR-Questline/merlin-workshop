using Awaken.TG.Main.Heroes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    
    [AddComponentMenu("VFX/Property Binders/Hero Velocity Binder")]
    [VFXBinder("AR/Hero Velocity")] 
    public class VFXHeroVelocityBinder : VFXBinderBase {
        [VFXPropertyBinding("UnityEngine.Vector3"), SerializeField]
        ExposedProperty property = "Velocity";

        public override bool IsValid(VisualEffect component) {
            return Hero.Current?.VHeroController != null && component.HasVector3(property);
        }

        public override void UpdateBinding(VisualEffect component) {
            component.SetVector3(property, Hero.Current.HorizontalVelocity);
        }

        public override string ToString() {
            return $"Hero Velocity : '{property}' -> {(Hero.Current?.HorizontalVelocity == null ? "(null)" : Hero.Current?.HorizontalVelocity)}";
        }
    }
}