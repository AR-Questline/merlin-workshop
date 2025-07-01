using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews {
    public interface IVEntryParentUI : IView {
        Transform EntriesParent { get; }
    }
}