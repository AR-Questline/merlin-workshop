#if true// USE_BATCH_RENDERER_GROUP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Rendering
{
#if UNITY_EDITOR
    [ExecuteAlways]
    [ExecuteInEditMode]
    public class EntitiesGraphicsStatsDrawer : MonoBehaviour
    {
        private bool m_Enabled = true;//false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                m_Enabled = !m_Enabled;
            }
        }

        private void OnGUI()
        {
            if (m_Enabled && World.DefaultGameObjectInjectionWorld != null)
            {
                var sys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();

                var stats = sys.Stats;

                GUILayout.BeginArea(new Rect { x = 10, y = 10, width = 500, height = 800 }, "Entities Graphics Stats", GUI.skin.window);

                GUILayout.Label("Culling stats (all viewports/callbacks):");
                GUILayout.Label($"  Chunks:\n    Total={stats.ChunkTotal}\n    AnyLOD={stats.ChunkCountAnyLod}\n    FullIn={stats.ChunkCountFullyIn}\n    w/Instance Culling={stats.ChunkCountInstancesProcessed}");
                GUILayout.Label($"  Instances tests: {stats.InstanceTests}");
                GUILayout.Label($"  Select LOD:\n    Total={stats.LodTotal}\n    No Requirements={stats.LodNoRequirements}\n    Chunks Tested={stats.LodChunksTested}\n    Changed={stats.LodChanged}");
                GUILayout.Label($"  Camera Move Distance: {stats.CameraMoveDistance} meters");
                GUILayout.Space(20);
                GUILayout.Label("Rendering stats (all viewports/callbacks):");
                GUILayout.Label($"  Batch Count: {stats.BatchCount}");
                GUILayout.Label($"  Rendered Instance Count: {stats.RenderedInstanceCount}");
                GUILayout.Label($"  Draw Range Count: {stats.DrawRangeCount}");
                GUILayout.Label($"  Draw Command Count: {stats.DrawCommandCount}");
                GUILayout.Label($"  GPU Memory:\n    Total={EditorUtility.FormatBytes(stats.BytesGPUMemoryUsed)}\n    Uploaded={EditorUtility.FormatBytes(stats.BytesGPUMemoryUploadedCurr)}\n    Max Uploaded={EditorUtility.FormatBytes(stats.BytesGPUMemoryUploadedMax)}");

                GUILayout.EndArea();
            }
        }
    }
#endif
}
#endif
