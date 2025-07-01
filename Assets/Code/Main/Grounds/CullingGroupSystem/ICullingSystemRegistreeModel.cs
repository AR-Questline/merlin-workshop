using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public interface ICullingSystemRegistree {
        Vector3 Coords { get; }
        
        /// <summary> Use Registree.Builder() for return </summary>
        Registree GetRegistree();

        void CullingSystemBandUpdated(int newDistanceBand);
    }
    
    /// <summary>
    /// A model that wants to register to the CullingSystem,
    /// automatically registered and unregistered to system upon adding and removing from world
    /// </summary>
    public interface ICullingSystemRegistreeModel : ICullingSystemRegistree, IGrounded {
        new Vector3 Coords { get; }
        
        public static class Events {
            public static readonly Event<ICullingSystemRegistreeModel, int> DistanceBandChanged = new(nameof(DistanceBandChanged));
            public static readonly Event<ICullingSystemRegistreeModel, bool> DistanceBandPauseChanged = new(nameof(DistanceBandPauseChanged));
        }
        
        Vector3 ICullingSystemRegistree.Coords => Coords;
        Vector3 IGrounded.Coords => Coords;
    }

    public static class CullingSystemRegistrees {
        public static int GetCurrentBand(this ICullingSystemRegistree registree) {
            return World.Services.Get<CullingSystem>().GetDistanceBand(registree);
        }
        public static int GetCurrentBandSafe(this ICullingSystemRegistree registree, int fallback) {
            return World.Services.TryGet<CullingSystem>()?.GetDistanceBandSafe(registree, fallback) ?? fallback;
        }
    }
}