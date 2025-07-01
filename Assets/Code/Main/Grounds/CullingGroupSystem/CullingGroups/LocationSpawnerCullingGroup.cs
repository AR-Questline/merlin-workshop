using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    public sealed class LocationSpawnerCullingGroup : BaseCullingGroup {
        public static bool InSpawnBand(int band) => World.Services.Get<SceneService>().IsOpenWorld ? band >= 3 : band >= 1;
        public static bool InAwayEnoughBand(int band) => band >= 3;
        
        static readonly float[] DistanceBands = {
            25,
            75,
            125,
            150,
        };
        public LocationSpawnerCullingGroup() : base(DistanceBands, 0, 1000) { }
    }
}