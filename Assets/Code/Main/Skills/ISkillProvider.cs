using System.Collections.Generic;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Skills {
    /// <summary>
    /// Interface required for all models that provide Skills for descriptions.
    /// </summary>
    public interface ISkillProvider : IModel {
        public IEnumerable<Skill> Skills { get; }
    }
}