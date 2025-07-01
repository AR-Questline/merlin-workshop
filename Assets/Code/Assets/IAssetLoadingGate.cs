using Awaken.TG.MVC;

namespace Awaken.TG.Assets {
    public interface IAssetLoadingGate {
        View OwnerView { get; } 
        
        bool TryLock();
        void Unlock();
    }
}