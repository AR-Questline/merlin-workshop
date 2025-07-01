using System;
using System.Collections.Generic;
using System.Globalization;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    [Serializable]
    public struct RendererWithVisibilityStats {
        [ReadOnly]
        public MaterialMeshNameWithoutLOD materialMesh;
        [ReadOnly]
        public string visibilityPercent;
        [Space(8)]
        [ReadOnly, ShowIf(nameof(isLOD))]
        public string[] lodVisibilityPercents;
        [ReadOnly] [ListDrawerSettings(HideRemoveButton = true, DraggableItems = false)] [SerializeReference] 
        public List<MeshRendererOrLODGroupHolder> renderers;
        [HideInInspector]
        public float visibilityPercentValue;
        [HideInInspector]
        public bool isLOD;

        public RendererWithVisibilityStats(MaterialMeshNameWithoutLOD materialMeshNameWithoutLOD, List<MeshRendererOrLODGroupHolder> renderers, float visibilityPercent, bool isLOD,
            string[] lodVisibilityPercents = null) : this() {
            this.materialMesh = materialMeshNameWithoutLOD;
            this.renderers = renderers;
            this.visibilityPercentValue = visibilityPercent;
            this.visibilityPercent = math.ceil(visibilityPercent * 100f).ToString(CultureInfo.InvariantCulture) + " %";;
            this.isLOD = isLOD;
            this.lodVisibilityPercents = lodVisibilityPercents;
        }
    }
}