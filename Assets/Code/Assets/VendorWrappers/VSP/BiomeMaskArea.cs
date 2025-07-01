using System.Collections.Generic;
using UnityEngine;

namespace AwesomeTechnologies.VegetationSystem
{
    [System.Serializable]
    public class Node
    {
        public bool Selected;
        public Vector3 Position;
        public bool OverrideWidth;
        public float CustomWidth = 2f;
        public bool Active = true;
        public bool DisableEdge;
    }

    public class BiomeMaskArea : MonoBehaviour
    {
        public List<Node> Nodes = new List<Node>();
        public bool ClosedArea = true;
        public bool ShowArea = true;
        public bool ShowHandles = true;
        public string MaskName = "";
        private bool _needInit;
        public string Id;
        public BiomeType BiomeType;
        public LayerMask GroundLayerMask;
        public AnimationCurve BlendCurve = new AnimationCurve();
        public AnimationCurve InverseBlendCurve = new AnimationCurve();

        public AnimationCurve TextureBlendCurve = new AnimationCurve();
        public float BlendDistance = 5f;
        public float NoiseScale = 20;
        public bool UseNoise = true;
    }
}