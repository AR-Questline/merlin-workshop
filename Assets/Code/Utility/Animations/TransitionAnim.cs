using Awaken.Utility.Animations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Awaken.Utility.Animations {
    [UnityEngine.Scripting.Preserve]
    public abstract class TransitionAnim : Anim {
        AnimationCurve _curve;

        public TransitionAnim(AnimationCurve curve) {
            _curve = curve;
        }

        protected override IEnumerable OnRun() {
            float time = 0f, current = 0f;
            float curveLen = _curve.keys[_curve.keys.Length - 1].time;
            while (time <= curveLen) {
                current = _curve.Evaluate(time);
                DoTransition(current);
                yield return null;
                time += Time.deltaTime;
            }
        }
        protected override void OnComplete() => DoTransition(1f);
        protected abstract void DoTransition(float theta);        
    }
}
