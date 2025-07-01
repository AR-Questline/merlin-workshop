using UnityEngine;

namespace Awaken.Utility.Animators {
    // based on: https://developpaper.com/re-implementation-of-unity3d-mixed-macanim-animation-2-2d-freeform-cartesian/
    public class CartesianBlendTree : CustomBlendTree {
        public string parameterX;
        public string parameterY;

        int _hashX;
        int _hashY;

        float[] _weights;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            _hashX = Animator.StringToHash(parameterX);
            _hashY = Animator.StringToHash(parameterY);

            _weights = new float[_children.Length];
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Vector2 position = new(animator.GetFloat(_hashX), animator.GetFloat(_hashY));
            
            float sum = 0;
            
            for (int i = 0; i < _children.Length; i++) {
                var weight = float.MaxValue;
                var iPosition = _children[i].Position;

                for (int j = 0; j < _children.Length; j++) {
                    if (i == j) continue;

                    var v0 = position - iPosition;
                    var v1 = _children[j].Position - iPosition;

                    var h = Mathf.Clamp01(1 - Vector3.Dot(v0, v1) / v1.sqrMagnitude);

                    weight = Mathf.Min(weight, h);
                }

                sum += weight;
                _weights[i] = weight;
            }

            if (sum == 0) {
                for (int i = 0; i < _children.Length; i++) {
                    _children[i].SetWeight(animator, 0);
                }
            } else {
                for (int i = 0; i < _children.Length; i++) {
                    _children[i].SetWeight(animator, _weights[i] / sum);
                }
            }
            
        }
    }
}