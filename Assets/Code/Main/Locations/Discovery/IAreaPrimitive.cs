using UnityEngine;

namespace Awaken.TG.Main.Locations.Discovery {
    public interface IAreaPrimitive {
        bool Contains(Vector3 point);
        Bounds Bounds { get; }
    }
}