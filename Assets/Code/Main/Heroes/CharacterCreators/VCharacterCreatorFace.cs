using Awaken.TG.Main.Heroes.CharacterCreators.Parts;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    [UsesPrefab("CharacterCreator/VCharacterCreatorFace")]
    public class VCharacterCreatorFace : VCharacterCreatorTab {
        [SerializeField] VCCPartHost faces;
        [SerializeField] VCCPartHost eyesColor;
        [SerializeField] VCCPartHost eyebrows;
        [SerializeField] VCCPartHost faceDetails;

        public override HeroRenderer.Target ViewTarget => HeroRenderer.Target.CCHead;

        protected override void OnFullyInitialized() {
            Add(CCGridSelectData.Faces, faces);
            Add(CCGridSelectData.EyeColor, eyesColor);
            Add(CCGridSelectData.Eyebrow, eyebrows);
            Add(CCGridSelectData.FacesDetails, faceDetails);
            ReceiveFocus();
        }
    }
}