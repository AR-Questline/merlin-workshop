using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class CustomGutsDeathBehaviour : MonoBehaviour, IDeathBehaviour {
        [SerializeField] bool disableObjectsToDisableAfterGutsSpawn;
        [SerializeField] GameObject[] objectsToDisable = Array.Empty<GameObject>();
        [ARAssetReferenceSettings(new [] {typeof(GameObject)}, true, AddressableGroup.NPCs), SerializeField]
        ShareableARAssetReference gutsPrefabRef;
        [SerializeField] bool snapAndRotateToGround;
        [SerializeField] float delayBeforeGutsSpawn;

        ARAsyncOperationHandle<GameObject> _gutsHandle;

        public bool UseDeathAnimation => false;
        public bool BlockExternalCustomDeath => true;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
        bool HasGutsPrefab => gutsPrefabRef is {IsSet: true};
        bool StillExists => this != null;

        public void OnVisualLoaded(DeathElement death, Transform transform) { }

        public void OnDeath(DamageOutcome damageOutcome, Location location) {
            if (!disableObjectsToDisableAfterGutsSpawn) {
                DisableObjectsToDisable();
            }
            
            // --- Spawn guts prefab
            if (HasGutsPrefab && StillExists) {
                if (delayBeforeGutsSpawn > 0) {
                    DelayInstantiateGuts(location).Forget();
                } else {
                    InstantiateGuts(location);
                }
            }
        }
        
        async UniTaskVoid DelayInstantiateGuts(Location location) {
            if (await AsyncUtil.DelayTime(location, delayBeforeGutsSpawn)) {
                if (StillExists) {
                    InstantiateGuts(location);
                }
            }
        }

        void InstantiateGuts(Location location) {
            var assetRef = gutsPrefabRef.Get();
            _gutsHandle = assetRef.LoadAsset<GameObject>();
            _gutsHandle.OnComplete(h => {
                if (this == null || h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                    ReleaseGutsHandle();
                    return;
                }

                var gutsInstance = Object.Instantiate(h.Result, transform);
                var gutsPosition = transform.position;
                if (snapAndRotateToGround) {
                    (float height, Vector3 groundNormal) = Ground.HeightAndNormalAt(gutsPosition, raycastMask: Ground.NpcGroundLayerMask, findClosest: Ground.FindClosestType.FindClosest);
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
                
                if (disableObjectsToDisableAfterGutsSpawn) {
                    DisableObjectsToDisable();
                }
            });
        }

        void DisableObjectsToDisable() {
            if (objectsToDisable != null) {
                foreach (var obj in objectsToDisable) {
                    if (obj != null) {
                        obj.SetActive(false);
                    }
                }
            }
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