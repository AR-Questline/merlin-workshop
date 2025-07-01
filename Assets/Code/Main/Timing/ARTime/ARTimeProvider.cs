using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Pathfinding;

namespace Awaken.TG.Main.Timing.ARTime {
    public sealed partial class ARTimeProvider : Element<NpcElement>, ITimeProvider {
        public override ushort TypeForSerialization => SavedModels.ARTimeProvider;
        public bool IsValid => ParentModel is { HasBeenDiscarded: false };
        public float GetDeltaTime() => ParentModel.GetDeltaTime();
        public float GetFixedDeltaTime() => ParentModel.GetFixedDeltaTime();
    }
}