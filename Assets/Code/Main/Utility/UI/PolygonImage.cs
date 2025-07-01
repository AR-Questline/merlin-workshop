using System.Collections.Generic;
using Awaken.TG.Utility.Maths;
using Awaken.Utility.Maths;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Image override that subdivides UI mesh for custom visual effects.
    /// </summary>
    [ExecuteAlways]
    public class PolygonImage : Image {
        public Vector2Int size = new Vector2Int(64, 64);
        
        protected override void OnPopulateMesh(VertexHelper toFill) {
            base.OnPopulateMesh(toFill);
            var count = size;
            var list = new List<UIVertex>();
            toFill.GetUIVertexStream(list);
            var min = list[0];
            var max = list[3];
            toFill.Clear();

            for (int x = 0; x <= count.x; x++)
            {
                for (int y = 0; y <= count.y; y++)
                {
                    var scale = new Vector3(x / (float)count.x, y / (float)count.y, 1f);
                    toFill.AddVert(Vector3Util.Lerp(min.position, max.position, scale), Color.white, Vector3Util.Lerp(min.uv0, max.uv0, scale),
                        scale, Vector3.forward, Vector2.right);
                }
            }
            for (int x = 0; x < count.x; x++)
            {
                for (int y = 0; y < count.y; y++)
                {
                    var lb = y + x * count.y + x;
                    var lt = lb + 1;
                    var rb = lb + count.y + 1;
                    var rt = rb + 1;
                    toFill.AddTriangle(lb, rb, lt);
                    toFill.AddTriangle(lt, rb, rt);
                }
            }
        }
    }
}
