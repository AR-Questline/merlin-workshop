using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Fights {
    public interface IAIEntity : IModel {
        public IWithFaction WithFaction { get; }

        public Vector3 VisionDetectionOrigin { get; }
        public VisionDetectionSetup[] VisionDetectionSetups { get; }
    }
}
