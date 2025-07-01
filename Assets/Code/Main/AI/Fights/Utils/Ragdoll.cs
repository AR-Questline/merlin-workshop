using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Utils {
    public class Ragdoll : MonoBehaviour {
        public Transform rigRoot;
        public float destroyAfterDistance = 100;
        VHeroController _heroController;

        void Awake() {
            _heroController = Hero.Current.VHeroController;
            InitLocSpec().Forget();
        }

        async UniTaskVoid InitLocSpec() {
            await UniTask.Delay(2000);
            Model model = GetComponent<LocationSpec>()?.CreateModel();
            if (!model?.IsInitialized ?? false) {
                World.Add(model);
            }
        }
        
        void Update() {
            Vector3 offset = _heroController.transform.position - transform.position;
            float sqrLen = offset.sqrMagnitude;
            if (sqrLen > destroyAfterDistance * destroyAfterDistance) {
                Destroy(gameObject);
            }
        }

        public void UpdateRig(List<Transform> root) {
            foreach (Transform originalBone in rigRoot.GetComponentsInChildren<Transform>()) {
                var bone = root.FirstOrDefault(b => b.name == originalBone.name);
                if (bone != null) {
                    originalBone.position = bone.position;
                    originalBone.rotation = bone.rotation;
                }
            }
        } 
    }
}
