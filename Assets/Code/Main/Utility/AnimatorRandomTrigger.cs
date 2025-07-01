using Awaken.TG.Main.General;
using UnityEngine;

namespace Awaken.TG.Main.Utility {
    /// <summary>
    /// Utility script used to play random animator clip every X..Y seconds.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimatorRandomTrigger : MonoBehaviour {

        public string triggerName = "random";
        public int triggersCount;
        public FloatRange delay;

        float _nextPlayTime;
        Animator _animator;

        void Awake() {
            _animator = GetComponent<Animator>();
            _nextPlayTime = Time.realtimeSinceStartup + delay.RandomPick();
        }

        void Update() {
            if (Time.realtimeSinceStartup >= _nextPlayTime) {
                Trigger();
                _nextPlayTime += delay.RandomPick();
            }
        }

        void Trigger() {
            string trigger = $"{triggerName}{Random.Range(1, triggersCount + 1).ToString()}";
            _animator.SetTrigger(trigger);
        }
    }
}