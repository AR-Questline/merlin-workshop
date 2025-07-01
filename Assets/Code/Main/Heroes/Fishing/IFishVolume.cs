using Awaken.TG.Main.Scenes.SceneConstructors;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Fishing {
    public interface IFishVolume {
        void OnGetVolume() { }
        float GetDensity(Vector3 position);
        ref readonly FishData FishData();
    }
    
    public class GenericFishVolume : IFishVolume {
        public static readonly GenericFishVolume Instance = new();
        
        public float GetDensity(Vector3 position) => CommonReferences.Get.FishingData.genericFishDensity;
        
        public ref readonly FishData FishData() {
            return ref CommonReferences.Get.FishingData.genericFishTable.GetRandomFish();
        }
    }
}