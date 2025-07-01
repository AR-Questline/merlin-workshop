using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class CustomGutsDeathBehaviour : MonoBehaviour, IDeathBehaviour {
        [SerializeField] GameObject[] objectsToDisable = Array.Empty<GameObject>();
        [ARAssetReferenceSettings(new [] {typeof(GameObject)}, true, AddressableGroup.NPCs), SerializeField]
        ShareableARAssetReference gutsPrefabRef;
        [SerializeField] bool snapAndRotateToGround;

        ARAsyncOperationHandle<GameObject> _gutsHandle;

        public bool UseDeathAnimation => false;
        public bool BlockExternalCustomDeath => true;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
        bool HasGutsPrefab => gutsPrefabRef is {IsSet: true};
        bool StillExists => this != null;

        public void OnVisualLoaded(DeathElement death, Transform transform) { }

        public void OnDeath(DamageOutcome damageOutcome, Location location) {
            // --- Disable objects to disable
            if (objectsToDisable != null) {
                foreach (var obj in objectsToDisable) {
                    if (obj != null) {
                        obj.SetActive(false);
                    }
                }
            }
            
            // --- Spawn guts prefab
            if (HasGutsPrefab && StillExists) {
                InstantiateGuts(location);
            }
        }

        void InstantiateGuts(Location location) {
            var assetRef = gutsPrefabRef.Get();
            _gutsHandle = assetRef.LoadAsset<GameObject>();
            _gutsHandle.OnComplete(h => {
                if (gameObject == null || h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                    ReleaseGutsHandle();
                }

                var gutsInstance = Object.Instantiate(h.Result, transform);
                var gutsPosition = transform.position;
                if (snapAndRotateToGround) {
                    (float height, Vector3 groundNormal) = Ground.HeightAndNormalAt(gutsPosition, raycastMask: Ground.NpcGroundLayerMask, findClosest: true);
                    gutsPosition.y = height;
                    gutsInstance.transform.up = groundNormal;
                }

                gutsInstance.transform.position = gutsPosition;
                gutsInstance.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                    linkedLifetime = true,
                    movable = false,
                });

                if (location != null) {
                    location.Initializer.OverridenLocationPrefab = assetRef;
                }
            });
        }

        void OnDestroy() {
            ReleaseGutsHandle();
        }

        void ReleaseGutsHandle() {
            if (_gutsHandle.IsValid()) {
                _gutsHandle.Release();
                _gutsHandle = default;
            }
        }
    }
}