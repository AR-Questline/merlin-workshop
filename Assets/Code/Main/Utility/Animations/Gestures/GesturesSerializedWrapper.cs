using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Gestures {
    [Serializable]
    public class GesturesSerializedWrapper {
        [SerializeField, TemplateType(typeof(GestureOverridesTemplate))] TemplateReference explicitOverrides;
        GestureOverridesTemplate _gestureOverrides;
        
        GestureOverridesTemplate GestureOverrides {
            get {
                if (_gestureOverrides == null) {
                    _gestureOverrides = explicitOverrides.TryGet<GestureOverridesTemplate>();
                }
                return _gestureOverrides;
            }
        }
        
        public GestureData? TryToGetGestureOverrideClipRef(string gestureKey) {
            GestureOverridesTemplate gestureOverridesTemplate = GestureOverrides;
            return gestureOverridesTemplate ? gestureOverridesTemplate.TryToGetAnimationClipRef(gestureKey) : null;
        }

        public void Preload(Story story) {
            GestureOverridesTemplate gestureOverridesTemplate = GestureOverrides;
            if (gestureOverridesTemplate == null) {
                return;
            }
            
            var gestureOverrides = gestureOverridesTemplate.gestureOverrides;
            gestureOverrides.Preload();

            story.ListenTo(Model.Events.BeforeDiscarded, () => {
                gestureOverrides.Unload();
            });
        }
    }
}