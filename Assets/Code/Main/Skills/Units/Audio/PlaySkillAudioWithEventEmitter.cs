using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.VisualScripts.Units;
using Awaken.Utility.Debugging;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Skills.Units.Audio {
    [UnitCategory("AR/Skills/Audio")]
    [TypeIcon(typeof(FlowGraph))]
    public class PlaySkillAudioWithEventEmitter : ARUnit, ISkillUnit {
        public const string SkillEmitterTag = "SkillEventEmitter";
        public ValueInput audioToPlay;
        
        protected override void Definition() {
            audioToPlay = ValueInput<EventReference>("AudioToPlay");
            DefineSimpleAction("Enter", "Exit", Enter);
        }
        
        void Enter(Flow flow) {
            EventReference audio = flow.GetValue<EventReference>(audioToPlay);
            
            if (audio.IsNull) return;

            Skill skill = this.Skill(flow);
            Transform parent = skill.Owner.ParentTransform;
            GameObject gameObject = new($"Audio Event Emitter {skill.ID}");
            gameObject.transform.SetParent(parent);
            gameObject.tag = SkillEmitterTag;
            ARFmodEventEmitter emitter = gameObject.AddComponent<ARFmodEventEmitter>();
            try {
                //emitter.StopEvent = EmitterGameEvent.ObjectDestroy;
                //emitter.PlayNewEventWithPauseTracking(audio);
            } catch (Exception e) {
                Log.Important?.Error($"Failed to play audio for skill: {this.Skill(flow).ID}");
                Log.Important?.Error(e.ToString());
            }
        }
    }
}