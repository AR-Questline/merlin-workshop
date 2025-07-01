using System.Collections.Generic;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    [UsesPrefab("HUD/Interactions/" + nameof(VStoryFakeHeroInteractionUI))]
    public class VStoryFakeHeroInteractionUI : View<StoryFakeHeroInteractionUI>, IUIPlayerInput {
        [SerializeField] InfoFrameData nameFrame;
        [SerializeField] InfoFrameData actionFrame;
        [SerializeField] InfoFrameData infoFrame1;
        [SerializeField] InfoFrameData infoFrame2;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnAlwaysVisibleHUD();
        public IEnumerable<KeyBindings> PlayerKeyBindings => KeyBindings.Gameplay.Interact.Yield();

        protected override void OnInitialize() {
            FillInfo().Forget();
            Target.ListenTo(Model.Events.AfterChanged, () => FillInfo().Forget(), this);
        }
        
        protected override void OnMount() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, Target));
        }

        async UniTaskVoid FillInfo() {
            string objectName = Target.DisplayName;
            bool isIllegal = Target.IsIllegal;
            nameFrame.FillFrame(new InfoFrame(objectName, false), isIllegal);
            actionFrame.FillFrame(Target.ActionFrame, isIllegal);
            infoFrame1.FillFrame(Target.InfoFrame1, isIllegal);
            infoFrame2.FillFrame(Target.InfoFrame2, isIllegal);
            
            // let layout groups recalculate
            var result = await AsyncUtil.DelayFrame(gameObject);
            if (result) {
                gameObject.SetActive(true);
            }
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction action && action.Name == KeyBindings.Gameplay.Interact) {
                Target.OnInteraction();
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }
    }
}