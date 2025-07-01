using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Utils;
using Awaken.TG.VisualScripts.Units;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class InstantiateARAssetReference : ARUnit, ISkillUnit {
        protected override void Definition() {
            var arAssetReference = RequiredARValueInput<ARAssetReference>("AssetReference");
            var pos = FallbackARValueInput("Position", _ => Vector3.zero);
            var rot = FallbackARValueInput("Rotation", _ => Quaternion.identity);
            var parent = FallbackARValueInput<Transform>("Parent", _ => null);
            DefineSimpleAction("Enter", "Exit", flow => {
                var assetRef = arAssetReference.Value(flow);
                Vector3 assetPos = pos.Value(flow);
                Quaternion assetRot = rot.Value(flow);
                Transform assetParent = parent.Value(flow);
                assetRef.LoadAsset<GameObject>().OnComplete(h => {
                    if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                        h.Release();
                        return;
                    }
                    var instance = Object.Instantiate(h.Result, assetPos, assetRot, assetParent);
                    instance.AddComponent<OnDestroyReleaseAsset>().Init(assetRef);
                });
            });
        }
    }
}