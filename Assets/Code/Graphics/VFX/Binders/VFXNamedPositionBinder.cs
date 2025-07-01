using System;
using System.Linq;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Named position Binder")]
    [VFXBinder("AR/Named position")]
    public class VFXNamedPositionBinder : VFXBinderBase {
        [Tags(TagsCategory.VFXTarget)]
        public string[] requiredTags = Array.Empty<string>();

        public string Property {
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set => property = value;
        }

        [VFXPropertyBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField]
        ExposedProperty property = "Position";
        Transform _target;

        Transform Target {
            get {
                if (!_target) {
                    _target = FindObjectsByType<VFXNamedTarget>(FindObjectsSortMode.None)
                        .FirstOrDefault(t => TagUtils.HasRequiredTags(t, requiredTags))
                        ?.transform;
                }

                return _target;
            }
        }

        public override bool IsValid(VisualEffect component) {
            return Target != null && component.HasVector3(property);
        }

        public override void UpdateBinding(VisualEffect component) {
            component.SetVector3(property, Target.position);
        }

        public override string ToString() {
            return $"Position : '{property}' -> {(Target == null ? "(null)" : Target.name)}";
        }
    }
}