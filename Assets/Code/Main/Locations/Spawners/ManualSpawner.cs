using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Spawners {
    public partial class ManualSpawner : Element<BaseLocationSpawner> {
        public sealed override bool IsNotSaved => true;

        public void TriggerSpawner() {
            ParentModel.SpawnPrefab().Forget();
        }
    }
}