using Awaken.TG.Assets;
using Awaken.TG.Main.UI.TitleScreen.ShadersPreloading;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class DebugVFXCollectionUtils {
        const string VfxCollectionAddress = "VfxCollection";
        static VfxCollection s_cachedCollection;
        
        /// <summary>
        /// Debug only method to get VFX collection by using wait for completion on the async operation handle, do not use in production code
        /// </summary>
        public static VfxCollection DEBUG_GetVfxCollectionWithWaitForCompletion() {
#if !AR_DEBUG && !DEBUG
            Awaken.Utility.Debugging.Log.Important?.Warning("VFX collection access is only available when AR_DEBUG or DEBUG is defined");
            return null;
#endif
            
            if (s_cachedCollection) {
                return s_cachedCollection;
            }
            
            var vfxAssetReference = new ShareableARAssetReference(VfxCollectionAddress);
            if (vfxAssetReference.IsSet) {
                var handle = vfxAssetReference.Get();
                var collection = handle.LoadAsset<VfxCollection>().WaitForCompletion();
                if (collection) {
                    s_cachedCollection = collection;
                    return collection;
                }
            }
            
            return null;
        }
    }
}