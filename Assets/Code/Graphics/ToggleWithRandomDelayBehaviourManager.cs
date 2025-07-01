using System;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Awaken.TG.Graphics {
    public class ToggleWithRandomDelayBehaviourManager : MonoBehaviour, UnityUpdateProvider.IWithUpdateGeneric {
        [SerializeField, HideInInspector] StructList<InstanceStateAndData> instances = StructList<InstanceStateAndData>.Empty;
        Random _random;

        void Awake() {
            unchecked {
                _random = new Random(50316659 * (uint)this.GetInstanceID());
            }
        }

        void OnEnable() {
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
        }

        void OnDisable() {
            UnityUpdateProvider.GetOrCreate().UnregisterGeneric(this);
        }

        public void UnityUpdate() {
            int count = instances.Count;
            var deltaTime = Time.deltaTime;
            for (int i = 0; i < count; i++) {
                ref var dataAndStateRef = ref instances[i];
                dataAndStateRef.elapsedTime += deltaTime;
                if (dataAndStateRef.elapsedTime > dataAndStateRef.delayTime) {
                    if (dataAndStateRef.gameObject != null) {
                        var newIsActive = !dataAndStateRef.isActive;
                        var delayRange = newIsActive ? dataAndStateRef.activeTimeMinMax : dataAndStateRef.inactiveTimeMinMax;
                        dataAndStateRef.elapsedTime = 0;
                        dataAndStateRef.delayTime = _random.NextFloat(delayRange.x, delayRange.y);
                        dataAndStateRef.isActive = newIsActive;
                        dataAndStateRef.gameObject.SetActive(newIsActive);
                    } else {
                        instances.RemoveAtSwapBack(i);
                        // Return because iteration would be invalid after removing middle element from array.
                        // We are just skipping one update of elapsed time, so not a big deal.
                        return;
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void EDITOR_Initialize(int count) {
            instances = new StructList<InstanceStateAndData>(count);
        }

        public void EDITOR_Add(ToggleWithRandomDelayBehaviour behaviour) {
            if (behaviour == null) {
                Log.Debug?.Error($"behaviour is null");
                return;
            }
            if (behaviour.target == null) {
                Log.Debug?.Error($"Target on behaviour {behaviour.name} is null", behaviour);
                return;
            }
            instances.Add(new InstanceStateAndData(behaviour.activeTimeMinMax, behaviour.inactiveTimeMinMax, behaviour.target));
        }
#endif

        [Serializable]
        struct InstanceStateAndData {
            public float elapsedTime;
            public float delayTime;
            public bool isActive;
            public Vector2 activeTimeMinMax;
            public Vector2 inactiveTimeMinMax;
            public GameObject gameObject;

            public InstanceStateAndData(Vector2 activeTimeMinMax, Vector2 inactiveTimeMinMax, GameObject target) {
                this.activeTimeMinMax = activeTimeMinMax;
                this.inactiveTimeMinMax = inactiveTimeMinMax;
                this.gameObject = target;
                elapsedTime = 0;
                delayTime = 0;
                isActive = target.activeSelf;
            }
        }
    }
}