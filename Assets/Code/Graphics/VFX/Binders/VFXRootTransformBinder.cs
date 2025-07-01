using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Graphics.VFX.Binders {
    /// <summary>
    /// Code inherited from UnityEngine.VFX.Utility.VFXTransformBinder
    /// </summary>
    [AddComponentMenu("VFX/Property Binders/Character Root Transform")]
    [VFXBinder("AR/Character Root Transform")]
    public class VFXRootTransformBinder : VFXBinderBase {
        [SerializeField, VFXPropertyBinding("UnityEditor.VFX.Transform")]
        ExposedProperty property = "Root";

        Transform _root;
        ExposedProperty _rotation;
        ExposedProperty _position;
        ExposedProperty _scale;
        bool _failedToInitialize;

        public string Property {
            [UnityEngine.Scripting.Preserve] get => (string)property;
            set {
                property = value;
                UpdateSubProperties();
            }
        }
        
        public override bool IsValid(VisualEffect component) {
            bool isPlaying = Application.isPlaying;
            if (!isPlaying) {
                _failedToInitialize = false;
                return false;
            }

            if (_failedToInitialize) {
                return false;
            }
            
            if (_root == null) {
                ICharacter character = VGUtils.GetModel<ICharacter>(component.gameObject);
                if (character == null) {
                    _failedToInitialize = true;
                    Log.Important?.Warning("Failed to Initialize VFXRootTransformBinder, there is no ICharacter here!", component.gameObject);
                    return false;
                }

                if (character is Hero h) {
                    _root = h.VHeroController.HeroAnimator.avatarRoot;
                } else if (character.Hips != null) {
                    _root = character.Hips.parent;
                }

                if (_root == null) {
                    return false;
                }
                
                UpdateSubProperties();
            }
            return component.HasVector3(_position) && component.HasVector3(_rotation) && component.HasVector3(_scale);
        }

        public override void UpdateBinding(VisualEffect component) {
            component.SetVector3(_position, _root.position);
            component.SetVector3(_rotation, _root.eulerAngles);
            component.SetVector3(_scale, _root.localScale);
        }
        
        public override string ToString() {
            return $"Transform : '{property}' -> {(_root == null ? "(null)" : _root.name)}";
        }

        protected override void OnDisable() {
            _root = null;
            base.OnDisable();
        }
        
        void UpdateSubProperties() {
            _position = property + "_position";
            _rotation = property + "_angles";
            _scale = property + "_scale";
        }
    }
}