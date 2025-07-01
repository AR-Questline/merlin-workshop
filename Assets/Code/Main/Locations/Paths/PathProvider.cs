using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Paths {
    [Serializable, InlineProperty]
    public class PathProvider {
        
        public bool embedded;

        [HideIf(nameof(embedded))] public VertexPathSpec vertexPathSpec;
        [ShowIf(nameof(embedded))] public List<Vector3> waypoints;

        [UnityEngine.Scripting.Preserve] public List<Vector3> Waypoints => embedded ? waypoints : vertexPathSpec?.waypoints;
        public bool IsNotNull => embedded || vertexPathSpec != null;
        
#if UNITY_EDITOR
        [Button, HideIf(nameof(embedded))]
        void CopyToEmbeddedList() {
            waypoints = vertexPathSpec.waypoints.ToList();
            embedded = true;
        }
        
        [NonSerialized, ShowInInspector, EnableIf(nameof(IsNotNull))] public bool edit;
#endif
    }
}