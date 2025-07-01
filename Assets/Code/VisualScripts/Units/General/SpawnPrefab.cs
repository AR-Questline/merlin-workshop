using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.General {
     [UnitCategory("AR/General")]
     [TypeIcon(typeof(GameObject))]
     [UnityEngine.Scripting.Preserve]
    public class SpawnPrefab : ARUnit {
        protected override void Definition() {
            var parent = FallbackARValueInput<Transform>("parent", _ => null);
            var position = FallbackARValueInput("position", _ => Vector3.zero);
            var rotation = FallbackARValueInput("rotation", _ => Quaternion.identity);
            var lifetime = InlineARValueInput("lifetime", 10);
            var prefabs = InlineARValueInput("prefabs", new List<GameObject>());
            DefineNoNameAction(flow => {
                var prefab = RandomUtil.UniformSelect(prefabs.Value(flow));
                var instance = PrefabPool.Instantiate(prefab, position.Value(flow), rotation.Value(flow), parent.Value(flow));
                instance?.Return(lifetime.Value(flow)).Forget();
            });
        }
    }
}