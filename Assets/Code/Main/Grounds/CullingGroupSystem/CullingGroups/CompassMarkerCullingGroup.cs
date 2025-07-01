using System.Linq;
using Awaken.TG.Main.General.Configs;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    public class CompassMarkerCullingGroup : BaseCullingGroup {
        public static bool MarkerActive(float farDistance, int bandIndex) => bandIndex < s_distanceBands.Length && farDistance >= s_distanceBands[bandIndex];
        public CompassMarkerCullingGroup() : base(s_distanceBands, 0.05f, 1000) { }
        
        static float[] s_distanceBands = GameConstants.Get.MapMarkersData
                                                      .Select(m => m.Value.farDistance)
                                                      .Distinct()
                                                      .OrderBy(f => f)
                                                      .ToArray();
    }
}