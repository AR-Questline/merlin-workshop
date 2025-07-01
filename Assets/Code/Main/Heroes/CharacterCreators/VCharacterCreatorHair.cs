using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.CharacterCreators.Parts;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    [UsesPrefab("CharacterCreator/VCharacterCreatorHair")]
    public class VCharacterCreatorHair : VCharacterCreatorTab {
        [SerializeField] VCCPartHost hairColor;
        [SerializeField] VCCPartHost hairs;
        [SerializeField] VCCPartHost beardColor;
        [SerializeField] VCCPartHost beard;

        public override HeroRenderer.Target ViewTarget => HeroRenderer.Target.CCHead;

        protected override void OnFullyInitialized() {
            Add(CCGridSelectData.Hairs, hairs);
            Add(CCGridSelectData.HairColor, hairColor);
            
            if (Target.ParentModel.GetGender() == Gender.Male) {
                Add(CCGridSelectData.Beards, beard);
                Add(CCGridSelectData.BeardColor, beardColor);
            } else {
                beardColor.TrySetActiveOptimized(false);
                beard.TrySetActiveOptimized(false);
            }

            ReceiveFocus();
        }
    }
}