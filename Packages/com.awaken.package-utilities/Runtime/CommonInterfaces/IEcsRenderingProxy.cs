using UnityEngine;

namespace Awaken.CommonInterfaces {
    public interface IEcsRenderingProxy {
        void ChangeLayer(int? layer, uint? renderingLayerMask);
    }

    public static class EcsRenderingProxyExtension {
        public static void SetEcsRenderingLayer(this GameObject instance, int? layer, uint? renderingLayerMask) {
            var renderingProxies = instance.GetComponentsInChildren<IEcsRenderingProxy>();
            foreach (var renderingProxy in renderingProxies) {
                renderingProxy.ChangeLayer(layer, renderingLayerMask);
            }
        }
    }
}
