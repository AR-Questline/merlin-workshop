using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.Gliding {
    [SpawnsView(typeof(VHeroGlideUI))]
    public partial class HeroGlideUI : Element<Hero>, IUniqueKeyProvider {
        public sealed override bool IsNotSaved => true;

        [UnityEngine.Scripting.Preserve] public IEnumerable<KeyBindings> PlayerKeyBindings => KeyBindings.Gameplay.Jump.Yield();
        public virtual KeyIcon.Data UniqueKey => new(KeyBindings.Gameplay.Jump, false);
    }
}
