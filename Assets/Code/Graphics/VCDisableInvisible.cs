using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Awaken.TG.Graphics {
    public class VCDisableInvisible : ViewComponent {
        const float UpdateFrequency = 1 / 10f;
        
        public new Collider collider;
        public GameObject[] toDisable = Array.Empty<GameObject>();
        public bool isMoving;

        Camera _camera;
        Camera Camera => _camera ??= World.Any<CameraStateStack>().MainCamera;

        Plane[] _planes = new Plane[6];
        Bounds _bounds;
        bool _isActive;
        bool _initialized;
        float _lastUpdate;

        protected override void OnAttach() {
            // Wait single frame for proper Unity initialization
            UniTask.NextFrame().ContinueWith(() => {
                GeometryUtility.CalculateFrustumPlanes(Camera, _planes);
                _bounds = collider.bounds;
                _isActive = toDisable[0].activeSelf;
                _initialized = true;
                _lastUpdate = Time.time - UpdateFrequency * 2;
            }).Forget();
        }

        void Update() {
            if (!_initialized) {
                return;
            }

            if (Time.time - _lastUpdate < UpdateFrequency) {
                return;
            }

            if (isMoving) {
                _bounds = collider.bounds;
            }
            
            GeometryUtility.CalculateFrustumPlanes(Camera, _planes);
            bool visible = GeometryUtility.TestPlanesAABB(_planes, _bounds);
            if (visible != _isActive) {
                _isActive = visible;
                toDisable.ForEach(g => g.SetActive(_isActive));
            }
        }
    }
}