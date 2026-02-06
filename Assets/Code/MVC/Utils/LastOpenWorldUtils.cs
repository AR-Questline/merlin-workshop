using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Extensions;

namespace Awaken.TG.MVC.Utils {
    public static class LastOpenWorldUtils {
        const string LastOpenWorldSceneKey = "LastOpenWorldScene";

        public static void SetLastVisitedWorld(SceneReference scene) {
            World.Services.TryGet<GameplayMemory>()?.Context().Set(LastOpenWorldSceneKey, scene.Name);
        }
        
        public static Worlds GetLastVisitedWorld() {
            var context = World.Services.Get<GameplayMemory>().Context();
            var lastOpenWorldSceneName = context.Get(LastOpenWorldSceneKey, string.Empty);
            var commonRefs = CommonReferences.Get;
            if (lastOpenWorldSceneName.Equals(commonRefs.CampaignReference.Name)) {
                return Worlds.HoS;
            } 
            if (lastOpenWorldSceneName.Equals(commonRefs.CampaignReference2.Name)) {
                return Worlds.Cuanacht;
            }
            if (lastOpenWorldSceneName.Equals(commonRefs.CampaignReference3.Name)) {
                return Worlds.Forlorn;
            }
            if (lastOpenWorldSceneName.Equals(commonRefs.SarrasCampaignSceneReference.Name)) {
                return Worlds.Sarras;
            }
            return Worlds.None;
        }

        public static bool WasLastOne(Worlds worlds) {
            if (worlds.Equals(Worlds.None)) {
                return true;
            }
            var lastWorld = GetLastVisitedWorld();
            if (lastWorld.Equals(Worlds.None)) {
                return false;
            }
            return worlds.HasFlagFast(lastWorld);
        }
        
        
        [Serializable, Flags]
        public enum Worlds : byte {
            None = 0,
            HoS = 1 << 0,
            Cuanacht = 1 << 1,
            Forlorn = 1 << 2,
            Sarras = 1 << 3,
        }
    }
}