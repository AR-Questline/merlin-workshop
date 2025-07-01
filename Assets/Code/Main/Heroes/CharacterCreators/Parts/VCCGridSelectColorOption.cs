using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    [UsesPrefab("CharacterCreator/Parts/VCCGridSelectColorOption")]
    public class VCCGridSelectColorOption : VCCGridSelectOption<CCGridSelectColorOption> {
        [SerializeField] Image icon;

        protected override void OnInitialize() {
            base.OnInitialize();
            icon.color = Target.Color;
        }
    }
}