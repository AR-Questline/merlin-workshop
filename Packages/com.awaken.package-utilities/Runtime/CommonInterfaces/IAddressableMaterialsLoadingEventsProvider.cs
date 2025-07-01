using UnityEngine;

namespace Awaken.CommonInterfaces {
    public interface IAddressableMaterialsLoadingEventsProvider {
        public event System.Action<string> OnStartedLoadingMaterial;
        public event System.Action<string> OnUnloadingMaterial;
        public event System.Action<string, Material> OnLoadedMaterial;
    }
}