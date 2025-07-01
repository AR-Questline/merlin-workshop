using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    [Serializable]
    public class GesturesSerializedWrapper {
        [SerializeField] GestureOverrides embedOverrides;
        
        [SerializeField, TemplateType(typeof(GestureOverridesTemplate))]
        TemplateReference explicitOverrides;
        
        static CommonReferences CommonReferences => World.Services.Get<CommonReferences>();
        
        public IEnumerable<GestureOverrides> GetAllOverrides(object debugTarget) {
            yield return embedOverrides;

            if (explicitOverrides is not { IsSet: true }) {
                yield break;
            }
            GestureOverridesTemplate template = explicitOverrides?.Get<GestureOverridesTemplate>(debugTarget);
            if (template != null) {
                yield return template.gestureOverrides;
            }
        }
        
        public AnimationClip TryToGetGestureOverrideClip(string gestureKey) {
            //Gestures priority: Embed Gestures > Explicit Gestures > Default Gestures (from Common References)
            AnimationClip embeddedClip = embedOverrides?.TryToGetAnimationClip(gestureKey);
            if (embeddedClip) {
                return embeddedClip;
            }

            if (explicitOverrides is {IsSet: true}) {
                GestureOverridesTemplate gestureOverridesTemplate = explicitOverrides.Get<GestureOverridesTemplate>();
                if (gestureOverridesTemplate) {
                    return gestureOverridesTemplate.TryToGetAnimationClip(gestureKey);
                }
            }

            return null;
        }

        public static AnimationClip TryToGetDefaultGesture(string gestureKey, Gender gender) {
            return CommonReferences.GenderGestures.TryGetValue(gender, out GestureOverridesTemplate gestureOverrides)
                ? gestureOverrides.TryToGetAnimationClip(gestureKey)
                : null;
        }

        public static GestureOverrides GetGenderSpecificGestures(Gender gender) {
            return CommonReferences.GenderGestures.TryGetValue(gender, out GestureOverridesTemplate template)
                ? template.gestureOverrides
                : null;
        }
    }
}