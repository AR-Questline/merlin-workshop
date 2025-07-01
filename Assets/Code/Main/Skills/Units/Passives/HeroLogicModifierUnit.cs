using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public abstract class HeroLogicModifierUnit : PassiveUnit {
        protected override void Definition() {}
        protected HeroLogicModifiers LogicModifiers => Hero.Current.LogicModifiers;

        public override void Enable(Skill skill, Flow flow) {
            SetActive(true);
        }

        public override void Disable(Skill skill, Flow flow) {
            SetActive(false);
        }
        
        protected abstract void SetActive(bool enable);
    }
}