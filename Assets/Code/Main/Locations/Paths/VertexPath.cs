using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Paths {
    [Serializable]
    public partial class VertexPath {
        public ushort TypeForSerialization => SavedTypes.VertexPath;

        [Saved] public List<Vector3> waypoints;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        VertexPath() {}
        
        public VertexPath(VertexPathSpec spec) {
            waypoints = spec.waypoints.ToList();
        }
    }
}