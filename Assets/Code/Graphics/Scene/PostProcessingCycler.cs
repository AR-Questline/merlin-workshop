using System;
using System.Linq;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Scene {
    public class PostProcessingCycler : MonoBehaviour {
        [ShowInInspector, DisplayAsString]
        public static int TotalPostProcesses { get; private set; } = 0;

        [ShowInInspector]
        public static int alternatePostProcessingMode = 0;

        [SerializeField] GameObject[] postProcessingPresets = Array.Empty<GameObject>();
        
        [ReadOnly]
        public int previousPostProcessing = 0;

        [UnityEngine.Scripting.Preserve]
        public GameObject CurrentPostProcess => postProcessingPresets[alternatePostProcessingMode];

        public event Action<GameObject> NewPostprocessActivated;

        void Awake() {
            postProcessingPresets ??= Array.Empty<GameObject>();
            postProcessingPresets = postProcessingPresets.WhereNotUnityNull().ToArray();
            
            if (postProcessingPresets.Length == 0) {
                enabled = false;
                return;
            }
            
            UpdateActivePP();
        }

        void Update() {
            TotalPostProcesses = postProcessingPresets.Length;
            if (previousPostProcessing != alternatePostProcessingMode) {
                UpdateActivePP();
            }
        }

        void UpdateActivePP() {
            if (postProcessingPresets.Length == 0) return;
            
            for (var index = 0; index < postProcessingPresets.Length; index++) {
                postProcessingPresets[index].SetActive(index == alternatePostProcessingMode);
            }
            NewPostprocessActivated?.Invoke(postProcessingPresets[alternatePostProcessingMode]);

            previousPostProcessing = alternatePostProcessingMode;
        }
    }
}