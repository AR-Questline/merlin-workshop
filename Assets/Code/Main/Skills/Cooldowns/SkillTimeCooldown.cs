using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Skills.Cooldowns {
    public partial class SkillTimeCooldown : DurationProxy<Skill>, ISkillCooldown {
        public override ushort TypeForSerialization => SavedModels.SkillTimeCooldown;

        [Saved] float _wholeTime;

        [JsonConstructor, UnityEngine.Scripting.Preserve] SkillTimeCooldown() { }
        public SkillTimeCooldown(float time) : base(new TimeDuration(time)) {
            _wholeTime = time;
        }

        public string GeneralDescription => LocTerms.TimeBasedCooldown.Translate(_wholeTime);
        public override IModel TimeModel => ParentModel.Owner;
    }
}