using Awaken.TG.Graphics.VFX;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    public class LightControllerCullingGroup : BaseCullingGroup {
        public LightControllerCullingGroup() : base(distanceBands, 0, 300) { }

        public static bool IsLightControllerUpdateBand(int bandIndex, LightController.LightSize lightSize) {
            return bandIndex <= ((int)lightSize);
        }
        
        static float[] distanceBands = {
            //0
            50f,
            //1
            150f,
            //2
            300f,
            //3
        };
    }
}