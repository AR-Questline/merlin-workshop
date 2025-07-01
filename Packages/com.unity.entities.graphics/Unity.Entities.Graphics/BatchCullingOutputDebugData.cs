#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine.Rendering;

namespace Unity.Rendering {
    public struct BatchCullingOutputDebugData {
        public Dictionary<MaterialMeshRef, long> materialMeshRefToVisibleCountMap;

        public BatchCullingOutputDebugData(int capacity) {
            materialMeshRefToVisibleCountMap = new Dictionary<MaterialMeshRef, long>(capacity);
        }

        public unsafe void FillBatchCullingDebugData(BatchCullingOutput cullingOutput, BatchRendererGroup brg, JobHandle generateCommandsDependency) {
            generateCommandsDependency.Complete();
            var drawCommands = cullingOutput.drawCommands[0];
            for (int i = 0; i < drawCommands.drawCommandCount; ++i)
            {
                var cmd = drawCommands.drawCommands[i];
                var material = brg.GetRegisteredMaterial(cmd.materialID);
                var mesh = brg.GetRegisteredMesh(cmd.meshID);
                var materialMeshRef = new MaterialMeshRef(material, mesh);
                materialMeshRefToVisibleCountMap.TryGetValue(materialMeshRef, out long visibleCount);
                visibleCount += cmd.visibleCount;
                materialMeshRefToVisibleCountMap[materialMeshRef] = visibleCount;
            }
        }
    }
}

#endif