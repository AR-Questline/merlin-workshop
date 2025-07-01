using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    public interface IAreaPrimitiveProvider {
        public static readonly Color Color = Color.yellow;
        
        IAreaPrimitive Spawn();
    }
}