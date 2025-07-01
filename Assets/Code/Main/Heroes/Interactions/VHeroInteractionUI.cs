using System;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    [UsesPrefab("HUD/Interactions/" + nameof(VHeroInteractionUI))]
    public class VHeroInteractionUI : View<IHeroInteractionUI> {
        [SerializeField] InfoFrameData nameFrame;
        [SerializeField] InfoFrameData actionFrame;
        [SerializeField] InfoFrameData infoFrame1;
        [SerializeField] InfoFrameData infoFrame2;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnAlwaysVisibleHUD();

        protected override void OnInitialize() {
            FillInfo().Forget();
            Target.ListenTo(Model.Events.AfterChanged, () => FillInfo().Forget(), this);
        }

        async UniTaskVoid FillInfo() {
            if (Target.Visible == false) {
                gameObject.SetActive(false);
                return;
            }
            
            string objectName = Target.Interactable.DisplayName;
            IHeroAction defaultAction = Target.Interactable.DefaultAction(Target.ParentModel);
            if (defaultAction == null) return;

            bool isIllegal = Target is HeroIllegalInteractionUI;
            
            nameFrame.FillFrame(new InfoFrame(objectName, false), isIllegal);
            actionFrame.FillFrame(defaultAction.ActionFrame, isIllegal);
            infoFrame1.FillFrame(defaultAction.InfoFrame1, isIllegal);
            infoFrame2.FillFrame(defaultAction.InfoFrame2, isIllegal);
            
            // let layout groups recalculate
            var result = await AsyncUtil.DelayFrame(gameObject);
            if (result) {
                gameObject.SetActive(true);
            }
        }
    }

    [Serializable]
    public struct InfoFrameData {
        [SerializeField] GameObject frameParent;
        [SerializeField] TextMeshProUGUI frameText;
        [SerializeField] GameObject buttonParent;

        public void FillFrame(InfoFrame frameInfo, bool isIllegal = false) {
            frameParent.SetActive(!string.IsNullOrEmpty(frameInfo.displayName) || frameInfo.isButtonActive);
            frameText.text = frameInfo.displayName ?? string.Empty;
            if (isIllegal) {
                frameText.color = ARColor.MainRed;
            }
            
            if (buttonParent) {
                buttonParent.SetActive(frameInfo.isButtonActive);
            }
        }
    }
}