using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    [UsesPrefab("CharacterCreator/Parts/VCCGridSelectIconOption")]
    public class VCCGridSelectIconOption : VCCGridSelectOption<CCGridSelectIconOption> {
        [SerializeField] Image icon;

        protected override void OnInitialize() {
            base.OnInitialize();
            if (Target.Icon is {IsSet: true}) {
                Target.Icon.RegisterAndSetup(this, icon);
            } else {
                gameObject.SetActive(false);
            }
        }

        protected override IBackgroundTask OnDiscard() {
            icon.sprite = null;
            return base.OnDiscard();
        }
    }
}