using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public struct AnimSlot {
        IAnimation _current;

        public bool Running => _current != null && !_current.Done;
        public event Action OnDone;

        [UnityEngine.Scripting.Preserve]
        public IAnimation Run(MonoBehaviour owner, IAnimation anim) {
            if (Running) {
                _current.ForceComplete();
                _current.OnDone -= AnimationDone;
            }
            _current = anim;
            _current.OnDone += AnimationDone;
            _current.Start(owner);
            return anim;
        }

        void AnimationDone() {
            OnDone?.Invoke();
            _current.OnDone -= AnimationDone;
        }
    }
}
