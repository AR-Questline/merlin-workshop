using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.Utility.Editor.Graphics {
    public class FallingRocksRaycast {
        const string VectorName = "Plane Collider_position";
        
        [MenuItem("CONTEXT/VisualEffect/Raycast Down")]
        public static void Raycast(MenuCommand command) {
            VisualEffect vfx = (VisualEffect)command.context;
            Physics.Raycast(vfx.transform.position, Vector3.down, out var hits);
            var height = hits.distance;
            vfx.SetVector3(VectorName, Vector3.up * -height);
        }
        
        [MenuItem("CONTEXT/VisualEffect/Raycast Down", true)]
        public static bool ValidateRaycast(MenuCommand command) {
            return ((VisualEffect)command.context).HasVector3(VectorName);
        }
    }
}