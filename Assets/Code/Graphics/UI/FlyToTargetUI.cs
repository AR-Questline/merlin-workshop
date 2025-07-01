using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Graphics.UI {
    [SpawnsView(typeof(VFlyToTargetUI))]
    public partial class FlyToTargetUI : Element<IModel> {
        public sealed override bool IsNotSaved => true;
        
        public TargetData Data { get; private set; }

        public FlyToTargetUI(TargetData data) {
            Data = data;
        }

        public class TargetData {
            public Sprite Sprite { get; private set; }
            public RectTransform Target { get; private set; }
            public Vector3 StartPosition { get; private set; }
            public Vector2 StartSize { get; private set; }
            public Vector2 EndSize { get; private set; }
            public float Time { get; private set; }
            public float OffsetMagnitude { get; private set; } = 1;
            public Vector3 Offset { get; private set; }

            [UnityEngine.Scripting.Preserve]
            public TargetData(Sprite sprite, RectTransform target, Vector3 startPosition, Vector2 startSize, float time) {
                Sprite = sprite;
                Target = target;
                StartPosition = startPosition;
                StartSize = startSize;
                EndSize = StartSize;
                Time = time;

                Offset = Random.onUnitSphere;
            }
            
            [UnityEngine.Scripting.Preserve]
            public TargetData(Sprite sprite, RectTransform target, Vector3 startPosition, Vector2 startSize, float offsetMagnitude, float time) {
                Sprite = sprite;
                Target = target;
                StartPosition = startPosition;
                StartSize = startSize;
                EndSize = StartSize;
                Time = time;
                OffsetMagnitude = offsetMagnitude;

                Offset = Random.onUnitSphere;
            }
            
            [UnityEngine.Scripting.Preserve]
            public TargetData(Sprite sprite, RectTransform target, Vector3 startPosition, Vector2 startSize, Vector2 endSize, float time) {
                Sprite = sprite;
                Target = target;
                StartPosition = startPosition;
                StartSize = startSize;
                EndSize = endSize;
                Time = time;

                Offset = Random.onUnitSphere;
            }
            
            [UnityEngine.Scripting.Preserve]
            public TargetData(Sprite sprite, RectTransform target, Vector3 startPosition, Vector2 startSize, Vector2 endSize, float offsetMagnitude, float time) {
                Sprite = sprite;
                Target = target;
                StartPosition = startPosition;
                StartSize = startSize;
                EndSize = endSize;
                Time = time;
                OffsetMagnitude = offsetMagnitude;

                Offset = Random.onUnitSphere;
            }
        }
    }
}