using System;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Graphics.Animations {
    public class SimpleTweens : MonoBehaviour {
        [SerializeField] Layer[] layers = Array.Empty<Layer>();

        void Start() {
            for (int i = 0; i < layers.Length; i++) {
                layers[i].Init();
            }
        }

        void LateUpdate() {
            var time = Time.time;
            for (int i = 0; i < layers.Length; i++) {
                layers[i].Update(time);
            }
        }

        [Serializable]
        struct Layer {
            [SerializeField] Transform transform;

            [SerializeField] Space space;
            
            [SerializeField, BoxGroup("Position"), InlineProperty] Tween positionX;
            [SerializeField, BoxGroup("Position"), InlineProperty] Tween positionY;
            [SerializeField, BoxGroup("Position"), InlineProperty] Tween positionZ;

            [SerializeField, BoxGroup("Rotation"), InlineProperty] Tween rotationX;
            [SerializeField, BoxGroup("Rotation"), InlineProperty] Tween rotationY;
            [SerializeField, BoxGroup("Rotation"), InlineProperty] Tween rotationZ;

            Vector3 _initialPosition;
            Vector3 _initialRotation;

            public void Init() {
                if (space == Space.World) {
                    transform.GetPositionAndRotation(out _initialPosition, out var initialRotationQuaternion);
                    _initialRotation = initialRotationQuaternion.eulerAngles;
                } else {
                    transform.GetLocalPositionAndRotation(out _initialPosition, out var initialRotationQuaternion);
                    _initialRotation = initialRotationQuaternion.eulerAngles;
                }
            }
            
            public void Update(float time) {
                var positionOffset = new Vector3(
                    positionX.Get(time),
                    positionY.Get(time),
                    positionZ.Get(time)
                );
                var rotationOffset = new Vector3(
                    rotationX.Get(time),
                    rotationY.Get(time),
                    rotationZ.Get(time)
                );
                if (space == Space.World) {
                    transform.SetPositionAndRotation(_initialPosition + positionOffset, Quaternion.Euler(_initialRotation + rotationOffset));
                } else {
                    transform.SetLocalPositionAndRotation(_initialPosition + positionOffset, Quaternion.Euler(_initialRotation + rotationOffset));
                }
            }
        }

        [Serializable]
        struct Tween {
            [SerializeField, HideLabel] TweenType type;
            [SerializeField, ShowIf(nameof(IsLinear))] float speed;
            [SerializeField, ShowIf(nameof(IsSine))] float amplitude;
            [SerializeField, ShowIf(nameof(IsSine))] float frequency;

            bool IsLinear => type == TweenType.Linear;
            bool IsSine => type == TweenType.Sine;
                
            public float Get(float time) {
                return type switch {
                    TweenType.None => 0f,
                    TweenType.Linear => time * speed,
                    TweenType.Sine => math.sin(time * frequency) * amplitude,
                    _ => 0f
                };
            }
        }

        enum TweenType : byte {
            None,
            Linear,
            Sine,
        }
    }
}