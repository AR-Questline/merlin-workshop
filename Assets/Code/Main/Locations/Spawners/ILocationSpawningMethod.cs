using System.Collections.Generic;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Spawners {
    public interface ILocationSpawningMethod : IElement {
        void Spawn(int currentBatchQuantitySpawned);
        int TargetSpawnCount();
        IEnumerable<LocationTemplate> GetAllPossibleTemplates();
    }
}