using System;
using System.Collections.Generic;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Behaviours {
    public class IdleBehavioursRefresher : MonoBehaviour, IService {
        const int FrameInterval = 3;

        readonly List<IdleBehaviours> _reusableBehaviourList = new();
        readonly HashSet<IdleBehaviours> _behavioursSet = new();

        int _refreshCount = -1;

        public void RequestRefresh(IdleBehaviours behaviours) {
            behaviours.NotifyHistorian("Interaction refresh requested");
            _behavioursSet.Add(behaviours);
        }

        public async UniTask WaitForNextUpdate() {
            int currentRefreshCount = _refreshCount;
            await UniTask.WaitUntil(() => _refreshCount > currentRefreshCount || _refreshCount == -1);
        }

        public void Cleanup() {
            _refreshCount = -1;
            _behavioursSet.Clear();
        }

        void LateUpdate() {
            if (World.EventSystem == null) {
                return;
            }
            
            if (!SceneLifetimeEvents.Get.EverythingInitialized || System.Object.ReferenceEquals(AstarPath.active, null)) {
                return;
            }
            
            // we want at least 2 frames between refreshes of the same IdleBehaviours
            // so Interaction can perform async operations within this 2 frames without any race conditions
            if (Time.frameCount % FrameInterval != 0) {
                return;
            }

            if (_behavioursSet.Count > 0) {
                _reusableBehaviourList.Clear();
                _reusableBehaviourList.AddRange(_behavioursSet);
                _behavioursSet.Clear();
                foreach (var behaviours in _reusableBehaviourList) {
                    try {
                        behaviours.InternalRefreshCurrentBehaviour();
                    } catch (Exception e) {
                        Log.Critical?.Error($"Exception below happened when refreshing {LogUtils.GetDebugName(behaviours)}", behaviours.ParentModel?.ParentTransform);
                        Debug.LogException(e);
                    }
                }
                _reusableBehaviourList.Clear();
            }

            _refreshCount++;
        }
    }
}