using Awaken.TG.Main.Character;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Skills {
    /// <summary>
    /// Interface required for all models that are parents to skills.
    /// </summary>
    public interface ISkillOwner : IModel {
        ICharacter Character { get; }
    }
}