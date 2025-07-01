using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.Utility;

namespace Awaken.TG.Main.Skills.Cooldowns {
    /// <summary>
    /// Cooldown for skills that can be used only once through whole game.
    /// </summary>
    public partial class OnlyOnceCooldown : Element<Skill>, ISkillCooldown {
        public override ushort TypeForSerialization => SavedModels.OnlyOnceCooldown;

        public string GeneralDescription => LocTerms.OnlyOnceCooldown.Translate();
        public bool Elapsed => false;
        public string DisplayText => "∞";
        public void Prolong(IDuration duration) { }
        public void Renew(IDuration duration) { }
        public void ResetDuration() { }
        [UnityEngine.Scripting.Preserve] public void ReduceTime(IDuration duration) { }
        public void ReduceTime(float percentage) { }
    }
}
