using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public abstract class TalentUnlockUnit : PassiveUnit {
        protected override void Definition() {}
        protected HeroDevelopment Development => Hero.Current?.Development;

        public override void Enable(Skill skill, Flow flow) {
            if (Development == null) return;
            SetActive(true);
        }

        public override void Disable(Skill skill, Flow flow) {
            if (Development == null) return;
            SetActive(false);
        }
        
        protected abstract void SetActive(bool enable);
    }
}