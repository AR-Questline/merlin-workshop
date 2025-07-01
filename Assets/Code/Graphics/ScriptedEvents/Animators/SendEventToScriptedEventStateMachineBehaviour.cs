using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Animators {
    public class SendEventToScriptedEventStateMachineBehaviour : ScriptedEventStateMachineBehaviour {
        [SerializeField] ScriptedEventEventType onEnter;
        [SerializeField] ScriptedEventEventType onExit;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            ScriptedEvent?.ReceiveEvent(onEnter);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            ScriptedEvent?.ReceiveEvent(onExit);
        }
    }
}