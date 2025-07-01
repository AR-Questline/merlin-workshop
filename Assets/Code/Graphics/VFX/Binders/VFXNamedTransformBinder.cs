using System;
using System.Linq;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Named transform Binder")]
    [VFXBinder("AR/Named transform")]
    public class VFXNamedTransformBinder : VFXBinderBase {
        [Tags(TagsCategory.VFXTarget)]
        public string[] requiredTags = Array.Empty<string>();

        public string Property {
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set {
                property = value;
                UpdateSubProperties();
            }
        }

        [VFXPropertyBinding("UnityEditor.VFX.Transform"), SerializeField]
        ExposedProperty property = "Transform";
        Transform _target = null;

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

        ExposedProperty _position;
        ExposedProperty _angles;
        ExposedProperty _scale;
        protected override void OnEnable() {
            base.OnEnable();
            UpdateSubProperties();
        }

        void OnValidate() {
            UpdateSubProperties();
        }

        void UpdateSubProperties() {
            _position = property + "_position";
            _angles = property + "_angles";
            _scale = property + "_scale";
        }

        public override bool IsValid(VisualEffect component) {
            return Target != null &&
                   component.HasVector3((int)_position) &&
                   component.HasVector3((int)_angles) &&
                   component.HasVector3((int)_scale);
        }

        public override void UpdateBinding(VisualEffect component) {
            component.SetVector3((int)_position, Target.transform.position);
            component.SetVector3((int)_angles, Target.transform.eulerAngles);
            component.SetVector3((int)_scale, Target.transform.lossyScale);
        }

        public override string ToString() {
            return $"Transform : '{property}' -> {(Target == null ? "(null)" : Target.name)}";
        }
    }
}