using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Stories {
    /// <summary>
    /// Enables snapshot that volumes down music and other while in dialogue.
    /// </summary>
    [UsesPrefab("UI/VHeroDialogueInvolvement")]
    public class VHeroDialogueInvolvement : View<HeroDialogueInvolvement> { }
}