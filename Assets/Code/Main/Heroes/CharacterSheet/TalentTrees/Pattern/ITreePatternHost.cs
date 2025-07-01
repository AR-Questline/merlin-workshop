using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern {
    public interface ITreePatternHost : IModel {
        Transform TreeParent { get; }
    }
}
