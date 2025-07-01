using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
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
    [AddComponentMenu("VFX/Property Binders/Character Transform")]
    [VFXBinder("AR/Character Transform")]
    public class VFXTransformBinder : VFXBinderBase {
        enum CharacterTransform {
            Hips,
            MainHand,
            OffHand
        }

        [SerializeField] CharacterTransform _characterTransformType;

        [SerializeField, VFXPropertyBinding("UnityEditor.VFX.Transform")]
        ExposedProperty property = "Kandra_KandraVertexStart";

        Transform _targetCharacterTransform;
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
            
            if (!component.gameObject.activeInHierarchy || component.transform.parent == null) {
                return false;
            }

            if (_targetCharacterTransform == null) {
                IWithItemSockets withItemSockets = VGUtils.TryGetModel<IWithItemSockets>(component.gameObject);
                if (withItemSockets == null) {
                    _failedToInitialize = true;
                    Log.Important?.Warning("Failed to Initialize VFXTransformBinder, there is no ICharacter here!",
                        component.gameObject);
                    return false;
                }

                _targetCharacterTransform = GetTransform(withItemSockets);

                if (_targetCharacterTransform == null) {
                    return false;
                }

                UpdateSubProperties();
            }

            return component.HasVector3(_position) && component.HasVector3(_rotation) && component.HasVector3(_scale);
        }

        Transform GetTransform(IWithItemSockets withSockets) {
            switch (_characterTransformType) {
                case CharacterTransform.Hips:
                    var hips = withSockets.HipsSocket;
                    return hips;
                case CharacterTransform.MainHand:
                    return withSockets.MainHandSocket;
                case CharacterTransform.OffHand:
                    return withSockets.OffHandSocket;
                default:
                    return withSockets.RootSocket;
            }
        }

        public override void UpdateBinding(VisualEffect component) {
            component.SetVector3(_position, _targetCharacterTransform.position);
            component.SetVector3(_rotation, _targetCharacterTransform.eulerAngles);
            component.SetVector3(_scale, _targetCharacterTransform.localScale);
        }

        public override string ToString() {
            return
                $"Transform : '{property}' -> {(_targetCharacterTransform == null ? "(null)" : _targetCharacterTransform.name)}";
        }

        protected override void OnDisable() {
            _targetCharacterTransform = null;
            _failedToInitialize = false;
            base.OnDisable();
        }

        void UpdateSubProperties() {
            _position = property + "_position";
            _rotation = property + "_angles";
            _scale = property + "_scale";
        }
    }
}