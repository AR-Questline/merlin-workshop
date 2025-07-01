using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    /// <summary>
    /// Instantiates UI material for chosen Graphic components (usually Image)
    /// Useful because Unity doesn't instantiate UI elements materials automatically by calling .material
    /// </summary>
    public class UIMaterialHelper : MonoBehaviour {

        // === References

        public Graphic[] affectedGraphics = Array.Empty<Graphic>();
        public Material material;

        public Material InstantiatedMaterial { get; private set; }
        [UnityEngine.Scripting.Preserve]
        public IEnumerable<Material> UsedMaterials => affectedGraphics.Select(graphic => graphic.materialForRendering);

        // === Unity lifecycle

        void Awake() {
            Init();
        }

        // === Logic

        protected virtual void Init() {
            if (affectedGraphics == null || affectedGraphics.Length == 0 || material == null) {
                AutoAssignReferences();
            }

            InstantiatedMaterial = Instantiate(material);
            foreach (Graphic graphic in affectedGraphics) {
                graphic.material = InstantiatedMaterial;
            }
        }

        void AutoAssignReferences() {
            if (affectedGraphics == null || affectedGraphics.Length == 0) {
                affectedGraphics = new Graphic[1];
                affectedGraphics[0] = GetComponent<Graphic>();
            }

            if (material == null) {
                material = affectedGraphics[0].material;
            }
        }
    }
}