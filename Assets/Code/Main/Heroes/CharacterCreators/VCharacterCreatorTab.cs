using System;
using Awaken.TG.Main.Heroes.CharacterCreators.Parts;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;

namespace Awaken.TG.Main.Heroes.CharacterCreators {
    public abstract class VCharacterCreatorTab : View<ICharacterCreatorTab>, IAutoFocusBase {
        protected CharacterCreator CharacterCreator => Target.ParentModel;

        public abstract HeroRenderer.Target ViewTarget { get; }
        
        protected override void OnInitialize() {
            CharacterCreator.HeroRenderer.SetViewTarget(ViewTarget);
        }

        protected void Add(Func<CharacterCreator, CCSliderData> provider, VCCPartHost view) {
            var slider = Target.AddElement(new CCSlider(provider, CharacterCreator));
            World.BindView(slider, view, true, true, false);
        }

        protected void Add(Func<CharacterCreator, CCGridSelectData> provider, VCCPartHost view) {
            var grid = Target.AddElement(new CCGridSelect(provider, CharacterCreator));
            World.BindView(grid, view, true, true, false);
            grid.AfterViewSpawned();
        }

        protected void Add(CCHeroName input, VCCPartHost view) {
            Target.AddElement(input);
            World.BindView(input, view, true, true, false);
        }

        protected void ReceiveFocus() {
            Target.Element<CharacterCreatorPart>().View<IVCCFocusablePart>().ReceiveFocusFromTop(0);
        }
    }
}