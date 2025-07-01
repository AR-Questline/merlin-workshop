using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using UnityEngine;

namespace Awaken.TG.Main.Stories {
    public partial class WyrdsphereVoid : Model {
        const float WyrdsphereSpawnDistance = 6f;

        public sealed override bool IsNotSaved => true;

        Location _wyrdsphereLocation;
        GameCameraVoidOverride _voidOverride;

        LocationTemplate WyrdSphereTemplate => CommonReferences.Get.WyrdSphereVoidTemplate.Get<LocationTemplate>();
        
        public override Domain DefaultDomain => Domain.CurrentScene();
        
        protected override void OnInitialize() {
            SpawnWyrdsphere();
            _voidOverride = World.Add(new GameCameraVoidOverride(true));
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            DestroyWyrdsphere();
            _voidOverride?.Discard();
            _voidOverride = null;
        }

        void SpawnWyrdsphere() {
            var forward = World.Only<GameCamera>().MainCamera.transform.forward;
            var wyrdspherePos = Hero.Current.Coords + forward * WyrdsphereSpawnDistance;
            
            _wyrdsphereLocation = WyrdSphereTemplate.SpawnLocation(wyrdspherePos, Quaternion.identity);
        }

        void DestroyWyrdsphere() {
            if (_wyrdsphereLocation is { HasBeenDiscarded: false }) {
                _wyrdsphereLocation.Discard();
                _wyrdsphereLocation = null;
            }
        }
    }
}