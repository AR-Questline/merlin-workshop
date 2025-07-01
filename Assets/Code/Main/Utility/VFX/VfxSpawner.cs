using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public partial class VfxSpawner : Element<Location>, IRefreshedByAttachment<VfxSpawnerAttachment>, ILogicReceiverElement {
        public override ushort TypeForSerialization => SavedModels.VfxSpawner;

        VfxSpawnerAttachment _spec;
        
        public void InitFromAttachment(VfxSpawnerAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            if (_spec.spawnOnInitialize) {
                SpawnVfx();
            }
        }
        
        protected override void OnRestore() {
            base.OnInitialize();
        }
        
        public void OnLogicReceiverStateChanged(bool state) {
            if ((_spec.spawnOnLogicEnable && state) || (_spec.spawnOnLogicDisable && !state)) {
                SpawnVfx();
            }
        }

        void SpawnVfx() {
            PrefabPool.InstantiateAndReturn(_spec.vfxEffect, Vector3.zero, Quaternion.identity, _spec.vfxLifetime, ParentModel.ViewParent).Forget();
        }
    }
}