using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Cameras {
    /// <summary>
    /// Main View of Game Camera. Keeps references and manages lifecycle of Camera Controllers.
    /// </summary>
    [UsesPrefab("VGameCamera")]
    public class VGameCamera : View<GameCamera> {
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnCamera();

        // === Mount & Init
        protected override void OnInitialize() {
            var cullingMask = Target.MainCamera.cullingMask;
            var excludedLayers = RenderLayers.Mask.MapMarker;
            cullingMask &= ~excludedLayers;
            Target.MainCamera.cullingMask = cullingMask;
            Target.MainCamera.useOcclusionCulling = false;
        }
    }
}