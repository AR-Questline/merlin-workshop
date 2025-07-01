using Awaken.TG.Utility.Attributes.Tags;
using System;
using Awaken.TG.Main.Stories;
using UnityEngine;

namespace Awaken.TG.Graphics.ScriptedEvents.Animators {
    public class ChangeFlagStateMachineBehaviour : StateMachineBehaviour {
        [SerializeField] Flag[] onEnter = Array.Empty<Flag>();
        [SerializeField] Flag[] onExit = Array.Empty<Flag>();

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            foreach (var flag in onEnter) {
                StoryFlags.Set(flag.flag, flag.value);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            foreach (var flag in onExit) {
                StoryFlags.Set(flag.flag, flag.value);
            }
        }

        [Serializable]
        struct Flag {
            [Tags(TagsCategory.Flag)] public string flag;
            public bool value;
        }
    }
}