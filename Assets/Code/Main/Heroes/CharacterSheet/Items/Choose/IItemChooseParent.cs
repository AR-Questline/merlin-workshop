using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose {
    public interface IItemChooseParent : IModel {
        IEnumerable<Item> PossibleItems { get; }
        Transform ChooseHost { get; }
        Prompts Prompts { get; }
    }
}