using System.Collections.Generic;

namespace Awaken.CommonInterfaces {
    public static class AddressableMaterialsLoadingEventsProviders {
        public static event System.Action<IAddressableMaterialsLoadingEventsProvider> OnNewProviderAdded; 
        public static event System.Action<IAddressableMaterialsLoadingEventsProvider> OnProviderRemoved; 
        public static List<IAddressableMaterialsLoadingEventsProvider> Providers { get; } = new(2);
        
        public static void AddProvider(IAddressableMaterialsLoadingEventsProvider provider) {
            if (Providers.Contains(provider) == false) {
                Providers.Add(provider);
                OnNewProviderAdded?.Invoke(provider);
            }
        }
        
        public static void RemoveProvider(IAddressableMaterialsLoadingEventsProvider provider) {
            if (Providers.Remove(provider)) {
                OnProviderRemoved?.Invoke(provider);
            }
        }
    }
}