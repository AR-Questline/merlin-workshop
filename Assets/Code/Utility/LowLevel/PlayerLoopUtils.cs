using System;
using Awaken.Utility.Debugging;
using UnityEngine.LowLevel;

namespace Awaken.Utility.LowLevel {
    public static class PlayerLoopUtils {
        delegate void AddSystem(ref PlayerLoopSystem[] destination, PlayerLoopSystem system);

        public static void RegisterToPlayerLoopEnd<T, TParent>(PlayerLoopSystem.UpdateFunction update, bool logAlreadyPresent = true)
            where TParent : struct {
            RegisterToPlayerLoop<T, TParent>(update, AddEnd, logAlreadyPresent);
        }

        public static void RegisterToPlayerLoopAfter<T, TParent, TBefore>(PlayerLoopSystem.UpdateFunction update, bool logAlreadyPresent = true)
            where TParent : struct where TBefore : struct {
            RegisterToPlayerLoop<T, TParent>(update, AddAfter<TBefore>, logAlreadyPresent);
        }

        public static void RegisterToPlayerLoopBegin<T, TParent>(PlayerLoopSystem.UpdateFunction update, bool logAlreadyPresent = true)
            where TParent : struct {
            RegisterToPlayerLoop<T, TParent>(update, AddBegin, logAlreadyPresent);
        }

        public static void RegisterToPlayerLoopEnd<T>(PlayerLoopSystem.UpdateFunction update) {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (Array.FindIndex(playerLoop.subSystemList, static s => s.type == typeof(T)) != -1) {
                Log.Minor?.Error($"System [{typeof(T).Name}] is already registered to player loop");
                return;
            }
            Array.Resize(ref playerLoop.subSystemList, playerLoop.subSystemList.Length + 1);
            playerLoop.subSystemList[^1] = new PlayerLoopSystem {
                type = typeof(T),
                updateDelegate = update,
            };
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void RegisterToPlayerLoopBegin<T>(PlayerLoopSystem.UpdateFunction update) {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (Array.FindIndex(playerLoop.subSystemList, static s => s.type == typeof(T)) != -1) {
                Log.Minor?.Error($"System [{typeof(T).Name}] is already registered to player loop");
                return;
            }
            Array.Resize(ref playerLoop.subSystemList, playerLoop.subSystemList.Length + 1);
            Array.Copy(playerLoop.subSystemList, 0, playerLoop.subSystemList, 1, playerLoop.subSystemList.Length - 1);
            playerLoop.subSystemList[0] = new PlayerLoopSystem {
                type = typeof(T),
                updateDelegate = update,
            };
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void RemoveFromPlayerLoop<T, TParent>() where TParent : struct {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var parentSystems = playerLoop.subSystemList;
            var parentIndex = Array.FindIndex(parentSystems, static s => s.type == typeof(TParent));
            if (parentIndex == -1) {
                Log.Critical?.Error($"Can remove [{typeof(T).Name}] system from player loop because parent [{typeof(TParent).Name}] system cannot be find");
            }
            ref var parentSystem = ref parentSystems[parentIndex];
            ref var subsystems = ref parentSystem.subSystemList;
            var toRemove = Array.FindIndex(subsystems, static s => s.type == typeof(T));
            if (toRemove == -1) {
                return;
            }
            Array.Copy(subsystems, toRemove+1, subsystems, toRemove, subsystems.Length - 1 - toRemove);
            Array.Resize(ref subsystems, subsystems.Length - 1);

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        static void RegisterToPlayerLoop<T, TParent>(PlayerLoopSystem.UpdateFunction update, AddSystem addSystem, bool logAlreadyPresent)
            where TParent : struct {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var parentSystems = playerLoop.subSystemList;
            var parentIndex = Array.FindIndex(parentSystems, static s => s.type == typeof(TParent));
            if (parentIndex == -1) {
                Log.Critical?.Error($"Can not register [{typeof(T).Name}] system to player loop because parent [{typeof(TParent).Name}] system cannot be find");
            }
            ref var parentSystem = ref parentSystems[parentIndex];

            ref var subsystems = ref parentSystem.subSystemList;
            if (Array.FindIndex(subsystems, static s => s.type == typeof(T)) != -1) {
                if (logAlreadyPresent) {
                    Log.Minor?.Error($"System [{typeof(T).Name}] is already registered to player loop");
                }
                return;
            }

            PlayerLoopSystem customSystem = new() {
                type = typeof(T),
                updateDelegate = update,
            };
            Array.Resize(ref subsystems, subsystems.Length + 1);
            addSystem(ref subsystems, customSystem);

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        static void AddEnd(ref PlayerLoopSystem[] destination, PlayerLoopSystem system) {
            destination[^1] = system;
        }

        static void AddBegin(ref PlayerLoopSystem[] destination, PlayerLoopSystem system) {
            Array.Copy(destination, 0, destination, 1, destination.Length - 1);
            destination[0] = system;
        }

        static void AddAfter<TBefore>(ref PlayerLoopSystem[] destination, PlayerLoopSystem system) {
            var index = Array.FindIndex(destination, static s => s.type == typeof(TBefore));
            if (index == -1) {
                Log.Critical?.Error($"Can not add system after [{typeof(TBefore).Name}] because it cannot be found");
                return;
            }
            Array.Resize(ref destination, destination.Length + 1);
            Array.Copy(destination, index + 1, destination, index + 2, destination.Length - index - 2);
            destination[index + 1] = system;
        }
    }
}
