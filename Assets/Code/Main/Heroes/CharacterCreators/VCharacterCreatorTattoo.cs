using Awaken.TG.Main.Heroes.CharacterCreators.Parts;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    [UsesPrefab("CharacterCreator/" + nameof(VCharacterCreatorTattoo))]
    public class VCharacterCreatorTattoo : VCharacterCreatorTab {
        [SerializeField] VCCPartHost faceTattoos;
        [SerializeField] VCCPartHost faceTattooColor;
        [SerializeField] VCCPartHost bodyTattoos;
        [SerializeField] VCCPartHost bodyTattooColor;
        
        public override HeroRenderer.Target ViewTarget => HeroRenderer.Target.CCBody;

        protected override void OnFullyInitialized() {
            Add(CCGridSelectData.FaceTattoo, faceTattoos);
            Add(CCGridSelectData.FaceTattooColor, faceTattooColor);
            Add(CCGridSelectData.BodyTattoo, bodyTattoos);
            Add(CCGridSelectData.BodyTattooColor, bodyTattooColor);
            ReceiveFocus();
        }
    }
}