using System;
using Awaken.CommonInterfaces;
using UnityEngine;

namespace Awaken.ECS.MedusaRenderer {
    public class MedusaRendererPrefab : MonoBehaviour, IRenderingOptimizationSystem,
        IRenderingOptimizationSystemTarget {
        public bool Has(UnityEngine.Renderer renderer) {
            throw new NotImplementedException();
        }
    }
}