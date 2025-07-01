#if UNITY_EDITOR
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public class RagdollPreviewer : MonoBehaviour {
        const int RigidbodyForceMultiplier = 25;
        const string BaseHumanPath = "Assets/3DAssets/Characters/Humans/Base_Male/Body/Prefabs/Prefab_BaseMale.prefab";
        const string PreviewHumanName = "SimpleInteractionPreview";
        public AnimationClip clip;
        public float animTimeStamp = 1f;
        public float delayPhysicsSimulation = 0f;
        public float forceToApply = 10f;
        GameObject _baseHumanAnimator;
        float _physicsDelay;
        float _lastFrameTime;

        [Button, PropertyOrder(100)]
        void SimulatePreview(GameObject customPreviewPrefab) {
            DestroyPreview();
            
            // --- Load Base Human
            GameObject baseHuman = InstantiatePreview(customPreviewPrefab != null ? customPreviewPrefab : AssetDatabase.LoadAssetAtPath<GameObject>(BaseHumanPath));
            _baseHumanAnimator = baseHuman.GetComponentInChildren<Animator>().gameObject;
            
            // --- Remove Alive Prefab
            var aliveTransform = _baseHumanAnimator.gameObject.FindChildRecursively("AlivePrefab", true);
            if (aliveTransform != null) {
                DestroyImmediate(aliveTransform.gameObject);
            }
            
            // --- Play Anim
            if (clip != null) {
                clip.SampleAnimation(_baseHumanAnimator.gameObject, animTimeStamp);
            }
            
            // --- Add Force
            if (forceToApply > 0) {
                var rootBone = baseHuman.FindChildWithTagRecursively("RootBone", true);
                if (rootBone != null) {
                    Rigidbody rb = rootBone.GetComponent<Rigidbody>();
                    rb.AddForceAtPosition(baseHuman.transform.forward * -1 * forceToApply * RigidbodyForceMultiplier, rb.position, ForceMode.Impulse);
                }
            }

            _physicsDelay = delayPhysicsSimulation;
            _lastFrameTime = Time.realtimeSinceStartup;
            EditorApplication.update += UpdatePhysics;
        }

        [Button, PropertyOrder(101)]
        void DestroyPreview() {
            EditorApplication.update -= UpdatePhysics;
            var existingPrefab = gameObject.FindChildRecursively(PreviewHumanName);
            if (existingPrefab != null) {
                DestroyImmediate(existingPrefab.gameObject, true);
            }
        }

        GameObject InstantiatePreview(GameObject previewPrefab) {
            GameObject preview = GameObject.Instantiate(previewPrefab, transform, false);
            preview.name = PreviewHumanName;
            preview.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
            return preview;
        }

        void UpdatePhysics() {
            float deltaTime = Time.realtimeSinceStartup - _lastFrameTime;
            _lastFrameTime = Time.realtimeSinceStartup;
            if (_physicsDelay > 0) {
                _physicsDelay -= deltaTime;
                return;
            }
            
            if (_baseHumanAnimator != null) {
                Physics.simulationMode = SimulationMode.Script;
                if (deltaTime > Time.fixedDeltaTime) {
                    int iterations = Mathf.FloorToInt(deltaTime / Time.fixedDeltaTime);
                    for (int i = 0; i < iterations; i++) {
                        Physics.Simulate(Time.fixedDeltaTime);
                        deltaTime -= Time.fixedDeltaTime;
                    }
                }
                Physics.Simulate(deltaTime);
            } else {
                EditorApplication.update -= UpdatePhysics;
                Physics.simulationMode = SimulationMode.FixedUpdate;
            }
        }
    }
}
#endif