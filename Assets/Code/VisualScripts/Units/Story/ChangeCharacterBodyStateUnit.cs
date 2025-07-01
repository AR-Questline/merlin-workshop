using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.VisualGraphUtils;

namespace Awaken.TG.VisualScripts.Units.Story {
    [UnityEngine.Scripting.Preserve]
    public class ChangeCharacterBodyStateUnit : ARUnit {
        protected override void Definition() {
            var inCharacter = FallbackARValueInput("character", VGUtils.My<ICharacter>);
            var inBodyState = FallbackARValueInput("newBodyState", _ => GeneralBodyState.RedDeath);
            DefineSimpleAction("Enter", "Exit", flow => {
                ICharacter character = inCharacter.Value(flow);
                GeneralBodyState newBodyState = inBodyState.Value(flow);
                if (character is Hero hero) {
                    hero.OnVisualLoaded(() => {
                        hero.BodyFeatures()?.ChangeBodyState(newBodyState);
                    });
                } else {
                    character?.BodyFeatures()?.ChangeBodyState(newBodyState);
                }
            });
        }
    }
}