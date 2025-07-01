using System;
using Awaken.TG.VisualScripts.Units;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Audio {
    [UnitCategory("AR/Audio")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class PlayAudioOneShot : ARUnit {
        public ValueInput audioToPlay;
        public FallbackValueInput<GameObject> attachToGameObject;
        
        protected override void Definition() {
            audioToPlay = ValueInput<EventReference>("AudioToPlay");
            attachToGameObject = FallbackARValueInput<GameObject>("AttachToGameObject", _ => null);
            DefineSimpleAction("Enter", "Exit", Enter);
        }
        
        void Enter(Flow flow) {
            EventReference audio = flow.GetValue<EventReference>(audioToPlay);
            GameObject attachTo = attachToGameObject.Value(flow);
            
            if (audio.IsNull) return;

            // --- Null-check is not enough as FMOD will throw an error when it cannot find the event.
            try {
                if (attachTo != null) {
                    //RuntimeManager.PlayOneShotAttached(audio, attachTo);
                } else {
                    //RuntimeManager.PlayOneShot(audio);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}