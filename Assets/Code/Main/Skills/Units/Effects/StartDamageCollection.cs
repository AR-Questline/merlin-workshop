using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.VisualScripts.Units;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnityEngine.Scripting.Preserve]
    public class StartDamageCollection : ARUnit, ISkillUnit {
        protected override void Definition() {
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            var effect = RequiredARValueInput<ShareableARAssetReference>("effect");
            var preventDamage = FallbackARValueInput("preventDamage", flow => false);

            ValueOutput("element", flow => {
                    return character.Value(flow).AddElement(new DamageCollectorElement(this.Skill(flow), effect.Value(flow), preventDamage.Value(flow)));
            });
        }
    }
}