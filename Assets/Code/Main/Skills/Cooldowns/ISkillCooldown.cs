using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Skills.Cooldowns {
    public interface ISkillCooldown : IElement<Skill>, IDuration {
        /// <summary>
        /// This description is used as part of skill descriptions.
        /// </summary>
        string GeneralDescription { get; }

        public void ApplyOn(Skill skill) {
            skill.AddElement(this);
            skill.Refresh();
            skill.Trigger(Skill.Events.CooldownAdded, this);
            this.ListenTo(Model.Events.AfterDiscarded, skill.Refresh, this);
        }
    }
}
