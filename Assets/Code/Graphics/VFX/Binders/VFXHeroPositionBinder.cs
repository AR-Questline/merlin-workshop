using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Hero position Binder")]
    [VFXBinder("AR/Hero position")]
    public class VFXHeroPositionBinder : VFXBinderBase {
        public string Property { 
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set => property = value;
        }

        [VFXPropertyBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField]
        ExposedProperty property = "Position";
        Transform _heroTarget = null;

        public override bool IsValid(VisualEffect component)
        {
            if (_heroTarget == null) {
                var view = Hero.Current?.VHeroController;
                if (!view) {
                    return false;
                }
                _heroTarget = view.transform;
            }
            return _heroTarget != null && component.HasVector3(property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(property, _heroTarget.transform.position);
        }

        public override string ToString()
        {
            return $"Position : '{property}' -> {(_heroTarget == null ? "(null)" : _heroTarget.name)}";
        }
    }
}