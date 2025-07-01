using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Graphics.Tweening
{
    public partial class TweenState
    {
        abstract class BaseTween : IStateTween {
            private string ChildName { get; }

            protected BaseTween(Component child) {
                ChildName = child.gameObject.name;
            }

            public void Enact(GameObject target, float duration, Func<Tweener, Tweener> chain = null) {
                Transform t = target.transform.Find(ChildName);
                var tweens = MakeTweens(t, duration);
                if (chain != null) {
                    foreach (var tween in tweens) chain(tween);
                }
            }

            protected abstract Tweener[] MakeTweens(Transform t, float duration);
        }

        class TransformTween : BaseTween {
            Vector3 _targetPos;
            Quaternion _targetRot;
            Vector3 _targetScale;

            TransformTween(Transform childTransform) : base(childTransform) {
                _targetPos = childTransform.localPosition;
                _targetRot = childTransform.localRotation;
                _targetScale = childTransform.localScale;
            }

            protected override Tweener[] MakeTweens(Transform t, float duration) {
                return new Tweener[] {
                    t.DOLocalMove(_targetPos, duration),
                    t.DOLocalRotateQuaternion(_targetRot, duration),
                    t.DOScale(_targetScale, duration)
                };
            }

            public static IStateTween Create(Transform fromChild) => new TransformTween(fromChild);
        }

        class SpriteSizeTween : BaseTween {
            Vector2 _targetSize;

            SpriteSizeTween(SpriteRenderer child) : base(child) {
                _targetSize = child.size;
            }

            protected override Tweener[] MakeTweens(Transform target, float duration) {
                SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
                return new[] {
                    DOTween.To(() => sr.size, (s) => sr.size = s, _targetSize, duration)
                };
            }

            public static IStateTween Create(Transform fromChild) {
                var sr = fromChild.GetComponent<SpriteRenderer>();
                return (sr != null) ? new SpriteSizeTween(sr) : null;
            }
        }

        class BoxColliderTween : BaseTween {
            Vector3 _center, _size;

            BoxColliderTween(BoxCollider child) : base(child) {
                _center = child.center;
                _size = child.size;
            }

            protected override Tweener[] MakeTweens(Transform t, float duration) {
                BoxCollider bc = t.GetComponent<BoxCollider>();
                return new[] {
                    DOTween.To(() => bc.size, (s) => bc.size = s, _size, duration),
                    DOTween.To(() => bc.center, (c) => bc.center = c, _center, duration),
                };
            }

            public static IStateTween Create(Transform fromChild) {
                var bc = fromChild.GetComponent<BoxCollider>();
                return (bc != null) ? new BoxColliderTween(bc) : null;
            }
        }

        // === Instantiators for tween types

        public delegate IStateTween TweenProp(Transform child);

        [UnityEngine.Scripting.Preserve]
        public static readonly TweenProp
            Transform = TransformTween.Create,
            SpriteSize = SpriteSizeTween.Create,
            BoxCollider = BoxColliderTween.Create;

        // === Fields

        List<IStateTween> _tweens = new List<IStateTween>();

        // === Constructors

        public TweenState(GameObject exemplar, params TweenProp[] enabledProps) {
            foreach (Transform child in exemplar.transform) {
                foreach (TweenProp prop in enabledProps) {
                    IStateTween tween = prop(child);
                    if (tween != null) _tweens.Add(tween);
                }
            }
        }

        // === Operation
        [UnityEngine.Scripting.Preserve]
        public void Enact(GameObject target, float duration, Func<Tweener, Tweener> chain) {
            foreach (var tween in _tweens) {
                tween.Enact(target, duration, chain);
            }            
        }
    }
}
