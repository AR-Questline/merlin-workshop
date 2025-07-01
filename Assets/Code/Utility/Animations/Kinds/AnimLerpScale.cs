using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Awaken.Utility.Animations.Anims {
    [UnityEngine.Scripting.Preserve]
    public class AnimLerpScale : Anim {

        float lerpFactor;
        Vector3 targetScale;

        public AnimLerpScale(float targetScale, float lerpFactor = 0.15f) {
            this.targetScale = Vector3.one * targetScale;
            this.lerpFactor = lerpFactor;
        }

        protected override IEnumerable OnRun() {
            Transform t = Owner.transform;
            LerpyVector lerpy = new LerpyVector(lerpFactor, t.localScale);
            Vector3 current;
            do {
                current = lerpy.UpdateAndGet(targetScale);
                t.localScale = current;
                yield return null;
            } while ((current - targetScale).sqrMagnitude > 0.01f);
        }

        protected override void OnComplete() {
            Owner.transform.localScale = targetScale;
        }
    }
}
