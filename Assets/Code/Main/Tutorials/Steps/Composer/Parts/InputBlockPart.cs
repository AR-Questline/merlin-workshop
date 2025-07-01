using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Universal;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class InputBlockPart : BasePart {
        [RichEnumExtends(typeof(KeyBindings))]
        public RichEnumReference[] keyCodes = new RichEnumReference[0];
        public bool allowHovering;
        public MouseClickEvent mouseClickEvent = new MouseClickEvent(false, false, new[] {0});
        public List<MonoBehaviour> allowedUIAware = new List<MonoBehaviour>();

        public override bool IsTutorialBlocker => true;

        IEnumerable<KeyBindings> Bindings => keyCodes?.Select(k => k.EnumAs<KeyBindings>());
        IEnumerable<string> ActionNames => Bindings?.Select(b => (string) b) ?? Enumerable.Empty<string>();
        
        public override async UniTask<bool> OnRun(TutorialContext context) {
            InputBlockerConfig config = new InputBlockerConfig {
                uiAware = allowedUIAware.OfType<IUIAware>(),
                actionNames = ActionNames,
                mouseEvent = mouseClickEvent,
                discardOnInputAccepted = true,
                allowHovering = allowHovering,
            };
            var blocker = new SelectiveInputBlocker(config);
            context.target.AddElement(blocker);
            while (!blocker.IsFullyInitialized) {
                await UniTask.NextFrame();
            }
            blocker.ListenTo(Model.Events.BeforeDiscarded, () => {
                context.Finish();
                World.EventSystem.RemoveAllListenersOwnedBy(context.vc);
            }, context.vc);
    
            // if tutorial finish comes from other place
            context.onFinish += () => {
                if (!blocker.WasDiscarded && !blocker.IsBeingDiscarded) {
                    blocker.Discard();
                }
            };

            return true;
        }
    }
}