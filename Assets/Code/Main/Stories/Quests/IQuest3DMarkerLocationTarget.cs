using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Views;

namespace Awaken.TG.Main.Stories.Quests {
    public interface IQuest3DMarkerLocationTarget : IGrounded {
        VLocation StickToReference { get; }
    }
}