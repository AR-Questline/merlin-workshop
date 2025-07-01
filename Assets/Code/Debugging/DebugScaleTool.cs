using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Debugging {
    public class DebugScaleTool : MonoBehaviour {

        [Range(0.01f, 10f)] public float placeScale = 1f;
        [Range(10f, 50f)] public float cameraPos = 10f;
        [Range(0.01f, 10f)] public float heroScale = 1f;

        float _placeScale = 1f;
        float _heroScale = 1f;
        float _cameraPos = 10f;
        Services _services;
        Camera _camera;

        void Update() {
            if (_services == null) {
                _services = World.Services;
                _camera = Camera.main;
                return;
            }

            if (placeScale != _placeScale && placeScale > 0.1f) {
                float multi = placeScale / _placeScale;
                foreach (Transform trans in World.All<Location>().Select(p => p.MainView.TryGrabChild<Transform>("Prefab"))) {
                    if (trans != null) {
                        trans.localScale *= multi;
                    }
                }
                _placeScale = placeScale;
            }

            if (heroScale != _heroScale) {
                float multi = heroScale / _heroScale;
                foreach (GameObject pawn in World.All<Hero>().Select(h => h.MainView.gameObject)) {
                    pawn.transform.localScale *= multi;
                }
                _heroScale = heroScale;
            }

            if (cameraPos != _cameraPos) {
                Vector3 pos = _camera.transform.position;
                pos.y = _cameraPos;
                _camera.transform.position = pos;
                _cameraPos = cameraPos;
            }
        }
    }
}