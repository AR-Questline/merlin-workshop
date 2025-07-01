using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Fights.Duels {
    public interface IDuelArena : IElement<Location> {
        UniTask Teleport(DuelistsGroup[] duelistsGroups, bool fadeOutAfterHeroTeleport);
        void Activate();
        void Deactivate();
    }
}
