using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Animators {
    public class ScriptedEventStateMachineBehaviour : StateMachineBehaviour, IScriptedEventHolder {
        public ScriptedEvent ScriptedEvent { get; set; }
    }
}