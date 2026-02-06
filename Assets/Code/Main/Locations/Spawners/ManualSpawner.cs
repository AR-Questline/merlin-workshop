using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Spawners {
    public partial class ManualSpawner : Element<BaseLocationSpawner> {
        public sealed override bool IsNotSaved => true;

        public async UniTask TriggerSpawner() {
            await ParentModel.SpawnPrefab();
        }
        
        public void UnlockAutomaticSpawning() {
            ParentModel.UnlockAutomaticSpawning();
            Discard();
        }
    }
}