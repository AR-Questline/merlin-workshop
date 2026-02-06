using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Skills that triggers on interaction.")]
    public class InteractionTriggerSkillsAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField]
        List<SkillReference> skills = new();
        [Space]
        public bool setAnimatorParametersOnTrigger = false;
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true), ShowIf(nameof(setAnimatorParametersOnTrigger))]
        AnimatorParameterData[] animatorParameters = Array.Empty<AnimatorParameterData>();
        
        public IEnumerable<SkillReference> Skills => skills;
        
        public void ApplyAnimatorParameters(AnimatorElement animator) {
            foreach (var param in animatorParameters) {
                animator.SetParameter(Animator.StringToHash(param.parameterName), param.targetParameter, param.shouldBeSaved);
            }
        }
        
        public Element SpawnElement() {
            return new InteractionTriggerSkills();
        }
        public bool IsMine(Element element) {
            return element is InteractionTriggerSkills;
        }
        
        // === Helpers
        [Serializable]
        public struct AnimatorParameterData {
            public SavedAnimatorParameter targetParameter;
            public string parameterName;
            public bool shouldBeSaved;
        }
    }
}