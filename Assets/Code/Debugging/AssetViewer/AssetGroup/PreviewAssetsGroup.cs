using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Debugging.AssetViewer.AssetGroup {

    public abstract class PreviewAssetsGroup<T> : PreviewAssetsGroupComponent where T : Template  {
        [SerializeField, InlineProperty, HideLabel, BoxGroup("Grid")] protected GridField grid;
        [SerializeField, InlineProperty, HideLabel, BoxGroup("Filters")] TemplatesFilter filters;

        public override void SpawnTemplates() {
            IEnumerable<T> allTemplates = World.Services.Get<TemplatesProvider>()
                .GetAllOfType<T>()
                .ToArray();
            T[] filteredTemplates = filters.FilterTemplates(allTemplates).ToArray();
            List<Vector3> slots = grid.GetSlots(transform, filteredTemplates.Length);

            for (int i = 0; i < filteredTemplates.Length; i++) {
                SpawnTemplate(filteredTemplates[i], slots[i]);
            }
        }

        public abstract void SpawnTemplate(T template, Vector3 position);


        
#if UNITY_EDITOR
        void OnDrawGizmos() {
            grid.DrawSlots(transform);
        }
#endif
    }
}