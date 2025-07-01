using System.Collections.Generic;
using System.Linq;
using Awaken.TG.VisualScripts.Units;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Audio {
    [UnitCategory("AR/Skills/Audio")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RemoveAllSkillEventEmitters : ARUnit, ISkillUnit {
        protected override void Definition() {
            DefineSimpleAction("Enter", "Exit", Enter);
        }

        void Enter(Flow flow) {
            Skill skill = this.Skill(flow);
            Transform parent = skill.Owner.ParentTransform;
            IEnumerable<Transform> emitters = parent.GetComponentsInChildren<Transform>(true)
                .Where(t => t.CompareTag(PlaySkillAudioWithEventEmitter.SkillEmitterTag));
            foreach (Transform skillEventEmitter in emitters) {
                StudioEventEmitter e = skillEventEmitter.GetComponent<StudioEventEmitter>();
                if (e != null) {
                    //e.Stop();
                }
                Object.Destroy(skillEventEmitter.gameObject);
            }
        }
    }
}