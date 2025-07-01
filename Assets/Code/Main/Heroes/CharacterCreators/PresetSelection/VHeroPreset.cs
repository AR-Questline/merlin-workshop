using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection {
    [UsesPrefab("CharacterCreator/PresetSelection/" + nameof(VHeroPreset))]
    public class VHeroPreset : View<HeroPreset> {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] TMP_Text description;
        [SerializeField] Image image;

        public ARButton Button => buttonConfig.button;
        public override Transform DetermineHost() => Target.ParentModel.View<VPresetSelector>().PresetButtonHost;
        
        Material _material;

        protected override void OnMount() {
            buttonConfig.InitializeButton(Target.SelectPreset, Target.Preset.name);
            buttonConfig.button.OnHover += OnHover;
            buttonConfig.button.OnSelected += OnSelect;
            description.text = Target.Preset.description;
            
            _material = new Material(image.material);
            image.material = _material;
            Target.Preset.icon.RegisterAndSetup(this, image);
            Highlight(false);
        }
        
        void OnHover(bool state) {
            if (RewiredHelper.IsGamepad) return;
            Highlight(state);
        }

        void OnSelect(bool state) {
            if (RewiredHelper.IsGamepad == false) return;
            Highlight(state);
        }
        
        void Highlight(bool state) {
            _material.DOFloat(state ? 0f : 1f, "_Grayscale", UITweens.ColorChangeDuration);
            image.DOGraphicColor(state ? Color.white : Color.grey, UITweens.ColorChangeDuration);
        }
    }
}