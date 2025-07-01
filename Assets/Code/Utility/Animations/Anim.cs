using System;
using System.Collections;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public class Anim : IAnimation {
        public event Action OnDone;
        public bool Done { get; private set; } = false;
        protected MonoBehaviour Owner { get; private set; }

        public void Start(MonoBehaviour owner) {
            Owner = owner;
            Owner.StartCoroutine(Run());
        }

        public IEnumerator Run() {
            foreach (var v in OnRun()) {
                if (Done) yield break;
                yield return v;
            }
            OnComplete();
            MarkDone();
        }

        public void ForceComplete() {
            OnComplete();
            MarkDone();
        }

        void MarkDone() {
            Done = true;
            OnDone?.Invoke();
        }

        protected virtual IEnumerable OnRun() { yield return null; }
        protected virtual void OnComplete() { }
    }
}
