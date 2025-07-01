using System.Linq;
using Awaken.Kandra;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Character.Features.Config;

namespace Awaken.TG.Main.Character.Features {
    public static class BlendShapeUtils {
        public static BlendShape[] RandomizeWithParams(BlendShapeConfigSO so, bool lockAsSelect = false, bool ignoreLock = true) {
            return so.configs
                .Where(config => config.Active && (ignoreLock || lockAsSelect == config.Locked))
                .Select(config => new BlendShape(config.targetBlendShape,
                    RandomUtil.NormalDistribution(config.targetPoint, config.bias, !config.Extremes)))
                .ToArray();
        }

        public static void ApplyShapes(KandraRenderer renderer, BlendShape[] shapes) {
            if (!renderer) {
                return;
            }
            renderer.EnsureInitialized();
            foreach (var shape in shapes) {
                var index = renderer.GetBlendshapeIndex(shape.name);
                if (index >= 0) {
                    renderer.SetBlendshapeWeight((ushort)index, shape.weight);
                }
            }
        }

        public static void RemoveShapes(KandraRenderer renderer, BlendShape[] shapes) {
            if (!renderer) {
                return;
            }

            if (renderer.BlendshapesCount > 0) {
                foreach (var shape in shapes) {
                    var index = renderer.GetBlendshapeIndex(shape.name);
                    if (index >= 0) {
                        renderer.SetBlendshapeWeight((ushort)index, 0);
                    }
                }
            }
        }
    }
}
