using System;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Graphics.Tweening
{
    public partial class TweenState
    {
        // === Various types of tweening

        public interface IStateTween {
            void Enact(GameObject target, float duration, Func<Tweener, Tweener> chain = null);
        }
    }
}
