using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using UnityEngine;

namespace Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization {
    [RequireComponent(typeof(MapScene))]
    public class BalanceTestArenaInitializer : MonoBehaviour {
        [SerializeField] ItemSet initialItemsSet;
        bool _itemSetSpawned;
        bool _isRestored;
#if UNITY_EDITOR
        void Start() {
            _isRestored = false;//GetComponent<MapScene>().TryRestoreWorld != null;
        }

        void Update() {
            if (_isRestored) {
                return;
            }
            
            if (_itemSetSpawned || initialItemsSet == null) {
                return;
            }

            if (Hero.Current == null || Hero.Current.VHeroController.HeroAnimator == null) {
                return;
            }
            
            initialItemsSet.ApplyFull();
            _itemSetSpawned = true;
        }
#endif
    }
}
