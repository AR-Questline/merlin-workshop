using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.Culling {
    public class DistanceCullerRenderer : DistanceCullerEntity {
        void OnDestroy() {
            World.Services.Get<DistanceCullersService>().Unregister(this);
        }

        // === DEBUG
#if DEBUG
        Renderer _renderer;
        Renderer Renderer => _renderer ? _renderer : _renderer = GetComponent<Renderer>();

        [FoldoutGroup(DebugFoldout), ShowInInspector] protected override Bounds CurrentBounds => !Renderer ? default : Renderer.bounds;
        [FoldoutGroup(DebugFoldout), ShowInInspector] protected override bool RenderingState => Renderer && Renderer.enabled;
#endif
    }
}
