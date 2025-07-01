using Awaken.ECS.MedusaRenderer;
using Awaken.ECS.Systems;
using Awaken.Kandra;
using Awaken.TG.LeshyRenderer;
using Awaken.TG.MVC;
using QFSW.QC;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCRenderingTools {
        [Command("toggle.leshy.enabled", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleLeshy() {
            foreach (var leshy in Object.FindObjectsByType<LeshyManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                leshy.enabled = !leshy.enabled;
            }
        }

        [Command("toggle.leshy.rendering", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleLeshyRendering() {
            foreach (var leshy in Object.FindObjectsByType<LeshyManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                leshy.EnabledRendering = !leshy.EnabledRendering;
            }
        }

        [Command("toggle.leshy.cells", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleLeshyCells() {
            var leshy = World.Services.Get<LeshyManager>();
            leshy.EnabledCells = !leshy.EnabledCells;
        }

        [Command("toggle.leshy.collider", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleLeshyCollider() {
            var leshy = World.Services.Get<LeshyManager>();
            leshy.EnabledCollider = !leshy.EnabledCollider;
        }

        [Command("toggle.leshy.loading", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleLeshyLoading() {
            var leshy = World.Services.Get<LeshyManager>();
            leshy.EnabledLoading = !leshy.EnabledLoading;
        }

        [Command("toggle.drake.enabled", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleDrake() {
            var disableAllRenderingSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<DisableAllRenderingSystem>();
            disableAllRenderingSystem.Enabled = !disableAllRenderingSystem.Enabled;
        }

        [Command("toggle.medusa", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleMedusa() {
            foreach (var medusa in Object.FindObjectsByType<MedusaRendererManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                medusa.enabled = !medusa.enabled;
            }
        }

        [Command("toggle.kandra.enabled", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleKandra() {
            var kandra = KandraRendererManager.Instance;
            kandra.enabled = !kandra.enabled;
        }

        [Command("toggle.kandra.rendering", "")][UnityEngine.Scripting.Preserve]
        public static void ToggleKandraRendering() {
            var kandra = KandraRendererManager.Instance.SkinnedBatchRenderGroup;
            kandra.enabled = !kandra.enabled;
        }
    }
}
