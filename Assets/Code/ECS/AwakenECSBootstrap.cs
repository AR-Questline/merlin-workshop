using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.LowLevel;
using Unity.Entities;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Awaken.ECS {
    public static class AwakenEcsBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() {
            DefaultWorldInitialization.Initialize("Default World");
            CreateEcsPlayerLoop();
        }
        
        public static void CreateEcsPlayerLoop() {
            DrakeRendererManager.Create();
            PlayerLoopUtils.RegisterToPlayerLoopEnd<DrakeRendererManager, Initialization>(DrakeRendererManager.InitializationUpdate, false);
        }
    }
}
