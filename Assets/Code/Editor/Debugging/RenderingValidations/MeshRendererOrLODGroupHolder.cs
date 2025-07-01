using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    [Serializable]
    public abstract class MeshRendererOrLODGroupHolder { }
    
    [Serializable]
    public sealed class MedusaMeshRendererHolder : MeshRendererOrLODGroupHolder {
        public MeshRenderer meshRenderer;

        public MedusaMeshRendererHolder(MeshRenderer meshRenderer) {
            this.meshRenderer = meshRenderer;
        }
    }

    [Serializable]
    public sealed class MedusaLODGroupHolder : MeshRendererOrLODGroupHolder {
        public LODGroup meshRenderer;

        public MedusaLODGroupHolder(LODGroup meshRenderer) {
            this.meshRenderer = meshRenderer;
        }
    }

    [Serializable]
    public sealed class DrakeLODGroupHolder : MeshRendererOrLODGroupHolder {
        public DrakeLodGroup lodGroup;

        public DrakeLODGroupHolder(DrakeLodGroup lodGroup) {
            this.lodGroup = lodGroup;
        }
    }

    [Serializable]
    public sealed class DrakeMeshRendererHolder : MeshRendererOrLODGroupHolder {
        public DrakeMeshRenderer meshRenderer;

        public DrakeMeshRendererHolder(DrakeMeshRenderer meshRenderer) {
            this.meshRenderer = meshRenderer;
        }
    }
}