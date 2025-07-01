using System;
using System.Collections.Generic;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.UI {
    public class CanvasService : MonoBehaviour, IService {
        [field: Required, SerializeField] public Canvas MainCanvas { get; private set; }
        [field: Required, SerializeField] public Canvas TooltipCanvas { get; private set; }
        [field: Required, SerializeField] public Canvas TutorialCanvas { get; private set; }
        [field: Required, SerializeField] public Canvas MapCompassCanvas { get; private set; }
        [field: Required, SerializeField] public Canvas HUDCanvas { get; private set; }
        [field: Required, SerializeField] public Canvas StickersCanvas { get; private set; }
        
        public float MainCanvasScaleFactor => MainCanvas.scaleFactor;
        
        public RectTransform MainTransform => (RectTransform) MainCanvas.transform;
        readonly Dictionary<string, RectTransform> _spawnRoots = new();

        Canvas[] _allCanvases = Array.Empty<Canvas>();
        
        public void HandleAspectRatioScaler(IModel target) {
            _allCanvases = new[] {
                MainCanvas,
                TooltipCanvas,
                TutorialCanvas,
                MapCompassCanvas,
                HUDCanvas,
                StickersCanvas
            };
            
            for (var i = 0; i < _allCanvases.Length; i++) {
                var canvas = _allCanvases[i];
                var scaler = canvas.gameObject.GetOrAddComponent<VCAspectRatioScaler>();
                
                if (scaler != null) {
                    scaler.Attach(World.Services, target, target.MainView);
                }
            }
        }
        
        public RectTransform CreateSpawnRoot(string name) {
            if (_spawnRoots.TryGetValue(name, out RectTransform root)) {
                if (root == null) {
                    _spawnRoots.Remove(name);
                    return CreateSpawnRoot(name);
                }
                
                return root;
            }

            root = new GameObject(name).AddComponent<RectTransform>();  
            root.SetParent(MainTransform);
            root.pivot = Vector2.zero;
            root.localPosition = Vector3.zero;
            _spawnRoots.Add(name, root);
            return root;
        }
        
        public bool DestroySpawnRoot(string name) {
            if (_spawnRoots.TryGetValue(name, out RectTransform root)) {
                if (root == null) {
                    return _spawnRoots.Remove(name);
                }
                
                Object.Destroy(root.gameObject);
                return _spawnRoots.Remove(name);
            }

            return false;
        }

        public void ShowTutorialCanvasOnly(bool state) {
            MainCanvas.enabled = !state;

            foreach (var nestedCanvas in MainCanvas.GetComponentsInChildren<Canvas>()) {
                nestedCanvas.enabled = !state;
            }
        }
    }
}